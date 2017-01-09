using Castle.DynamicProxy;
using Common.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public interface IExceptionHandler
    {
        ILog Logger
        {
            get;
            set;
        }
        void HandleExceptions(Action synchronousInvoker, IInvocation invocation);
        Task HandleExceptions(Func<Task> awaitableInvoker, IInvocation invocation);
        Task<T> HandleExceptions<T>(Func<Task<T>> awaitableInvoker, IInvocation invocation);
    }

    public interface IMethodSelectorForLogging
    {
        bool ShouldLogMethod(string methodName);
    }

    public class QueueMethodSelectorForLogging : IMethodSelectorForLogging
    {
        private static bool loggingEnabled = false;

        public static void EnableQueueLogging()
        {
            loggingEnabled = true;
        }

        public static void DisableQueueLogging()
        {
            loggingEnabled = false;
        }

        public bool ShouldLogMethod(string methodName)
        {
            if (loggingEnabled &&
                (methodName.StartsWith("GetMessageAsync") || methodName.StartsWith("PutMessageAsync")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class DefaultExceptionHandler : IExceptionHandler
    {
        public ILog Logger
        {
            get;
            set;
        }

        public DefaultExceptionHandler(ILog logger)
        {
            Logger = logger;
        }

        public static string GetMethodParamsStringRep(object[] arguments)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < arguments.Count(); i++)
            {
                builder.Append(string.Format("Argument No{0}: ", i));
                builder.Append(JsonConvert.SerializeObject(arguments[i]));
            }

            return builder.ToString();
        }



        public async Task LogException(IInvocation invocation, Exception exp)
        {
            try
            {
                var methodParams = GetMethodParamsStringRep(invocation.Arguments);
                await Logger.WriteInfo(string.Empty, string.Empty, string.Empty, string.Format("MethodName: {0}, Method Params: {1}, Exception {2}",
                    invocation.Method.Name, methodParams, exp?.ToString()), DateTime.UtcNow);
            }
            catch (Exception e)
            { }
        }

        public void HandleExceptions(Action synchronousInvoker, IInvocation invocation)
        {
            try
            {
                synchronousInvoker.Invoke();
            }
            catch (Exception exp)
            {
                Task.Run(async () => await LogException(invocation, exp));
                throw exp;
            }
        }

        public Task HandleExceptions(Func<Task> awaitableInvoker, IInvocation invocation)
        {
            try
            {
                return awaitableInvoker.Invoke();
            }
            catch (Exception exp)
            {
                Task.Run(async () => await LogException(invocation, exp));
                throw exp;
            }
        }
        public Task<T> HandleExceptions<T>(Func<Task<T>> awaitableInvoker, IInvocation invocation)
        {
            try
            {
                return awaitableInvoker.Invoke();
            }
            catch (Exception exp)
            {
                Task.Run(async () => await LogException(invocation, exp));
                throw exp;
            }
        }
    }

    public class AsyncLoggingWithExceptionInterceptor : IInterceptor
    {
        private readonly IExceptionHandler _handler;
        private readonly IMethodSelectorForLogging _selector;

        private static readonly MethodInfo handleAsyncMethodInfo =
            typeof(AsyncLoggingWithExceptionInterceptor).GetMethod("HandleAsyncWithResult", BindingFlags.Instance | BindingFlags.NonPublic);

        public void LogNormalReturn(IInvocation invocation, object retValue)
        {
            try
            {
                var methodParams = DefaultExceptionHandler.GetMethodParamsStringRep(invocation.Arguments);
                var ret = JsonConvert.SerializeObject(retValue ?? string.Empty);
                _handler.Logger.WriteInfo(string.Empty, string.Empty, string.Empty, string.Format("MethodName: {0}, Method Params: {1}, Return Value: {2}",
                    invocation.Method.Name, methodParams, ret), DateTime.UtcNow);
            }
            catch (Exception e)
            { }
        }

        public AsyncLoggingWithExceptionInterceptor(IExceptionHandler handler, IMethodSelectorForLogging selector)
        {
            _handler = handler;
            _selector = selector;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!_selector.ShouldLogMethod(invocation.Method.Name))
            {
                invocation.Proceed();
            }
            else
            {
                var delegateType = GetDelegateType(invocation);
                if (delegateType == MethodType.Synchronous)
                {
                    _handler.HandleExceptions(() => invocation.Proceed(), invocation);
                }
                if (delegateType == MethodType.AsyncAction)
                {
                    invocation.Proceed();
                    invocation.ReturnValue = HandleAsync((Task)invocation.ReturnValue, invocation);
                }
                if (delegateType == MethodType.AsyncFunction)
                {
                    invocation.Proceed();
                    ExecuteHandleAsyncWithResultUsingReflection(invocation);
                }

                LogNormalReturn(invocation, invocation.ReturnValue);
            }
        }

        private void ExecuteHandleAsyncWithResultUsingReflection(IInvocation invocation)
        {
            var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
            var mi = handleAsyncMethodInfo.MakeGenericMethod(resultType);
            invocation.ReturnValue = mi.Invoke(this, new[] { invocation.ReturnValue, invocation });
        }

        private async Task HandleAsync(Task task, IInvocation invocation)
        {
            await _handler.HandleExceptions(async () => await task, invocation);
        }

        private async Task<T> HandleAsyncWithResult<T>(Task<T> task, IInvocation invocation)
        {
            return await _handler.HandleExceptions(async () => await task, invocation);
        }

        private MethodType GetDelegateType(IInvocation invocation)
        {
            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
                return MethodType.AsyncAction;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return MethodType.AsyncFunction;
            return MethodType.Synchronous;
        }

        private enum MethodType
        {
            Synchronous,
            AsyncAction,
            AsyncFunction
        }
    }
}

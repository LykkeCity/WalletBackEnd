using System;
using Common;

namespace MvcUtils
{
    public class CheckDataException :Exception
    {
        public CheckDataException(string divId, string msg, PopoverPosition popoverPosition = PopoverPosition.Left):base(msg)
        {
            DivId = divId;
            PopoverPosition = popoverPosition;
        }

        public string DivId { get; private set; }

        public PopoverPosition PopoverPosition { get; private set; }
    }

    public static class JsonResultHelper
    {
        public const string StatusOk = "Ok";
        public const string StatusNavigate = "Navigate";
        public const string StatusRequest = "Request";
        public static string StatusRefresh  {get { return "Refresh"; }}
        public static string StatusRedirect { get { return "Redirect"; } }

        /// <summary>
        /// Если после положительного ответа необходимо вернуться на предыдущую страницу
        /// </summary>
        /// <returns></returns>
        public static object JsonOkResult()
        {
            return new { Status = StatusOk };
        }

        public static object JsonApiOk(object data = null)
        {
            if (data == null)
            return new { Status = StatusOk };

            return new {Status = StatusOk, Data = data};
        }

        public static object JsonApiFail(string reason)
        {
            return new { Status = "Fail", Reason = reason };
            
        }


        /// <summary>
        /// При положительном ответе, после чего необходимо инициализироваться
        /// </summary>
        /// <returns>JSON</returns>
        public static object JsonOkAndInit()
        {
            return new { Status = StatusOk, NavigateTo = "Init" };
        }

        /// <summary>
        /// При положительном ответе на реакцию которого не нужно реагировать. Например когда необходимо сохранить какое то состояние внутри окна
        /// </summary>
        /// <returns>JSON</returns>
        public static object JsonOkAndNothing()
        {
            return new {Status = "OkAndNothing"};
        }

        /// <summary>
        /// Если при положительном ответе мы хотим направить
        /// </summary>
        /// <param name="navigateTo">направляем на страницу</param>
        /// <param name="pages">набор страниц, которые будут участвовать в работе</param>
        /// <returns>JSON</returns>
        public static object JsonOkResult(string navigateTo, object pages)
        {
            return new { Status = StatusOk, NavigateTo = navigateTo, Pages = pages };
        }

        /// <summary>
        /// Если при положительном ответе мы хотим направить пользователя на другую страницу
        /// </summary>
        /// <param name="navigateTo">URL страницы</param>
        /// <returns>Ответ</returns>
        public static object JsonOkNavigate(string navigateTo)
        {
            return new { Status = StatusOk, NavigateTo = navigateTo };
        }

        /// <summary>
        /// Если при положительном ответе мы хотим направить человека на другую страницу
        /// </summary>
        /// <param name="navigateTo">направляем на страницу</param>
        /// <param name="prms"></param>
        /// <param name="hideHeader">Спрятать меню после запроса</param>
        /// <returns>JSON</returns>
        public static object JsonOkAndNavigate(string navigateTo, object prms = null, bool hideHeader = false)
        {

            if (prms == null)
                return new { Status = StatusNavigate, Url = navigateTo, hideHeader };

            var paramsAsStr = prms as string ?? prms.ToUrlParamString();

            return new { Status = StatusNavigate, Url = navigateTo, Params = paramsAsStr, hideHeader };
        }

        /// <summary>
        /// Сделать запрос на сервер и сервер распорядится как продолжать работать дальше через Json результат
        /// </summary>
        /// <param name="url">Url запроса</param>
        /// <param name="parameters">Параметры запроса, если они есть</param>
        /// <param name="putToHistory">положить запрос в историю браузера</param>
        /// <returns></returns>
        public static object Request(string url, object parameters = null, bool putToHistory = false)
        {
            var prms = parameters as string ?? parameters.ToUrlParamString();
            return new { Status = StatusRequest, url, prms, putToHistory};
        }

        public static object JsonOkAndRedirect(string navigateTo, string prms = null)
        {
            return new { Status = StatusRedirect, Url = navigateTo, Params = prms};
        }

        public static object JsonOkDialog(string url, object prms = null)
        {
            if (prms == null)
                return new { Status = StatusOk, Url = url };

            return new { Status = StatusOk, Url = url, Params = prms.ToUrlParamString()};
        }

        /// <summary>
        /// Если при положительном ответе мы хотим частично обновить страницу
        /// </summary>
        /// <param name="divId">Id DOM элемента</param>
        /// <param name="url">Url частичной страницы</param>
        /// <param name="parameters">Параметры</param>
        /// <param name="putToHistory">Показываем что запрос идет в историю браузер</param>
        /// <returns>Ответ</returns>
        public static object JsqonOkRefershPartly(string divId, string url, object parameters, bool putToHistory = false)
        {
            var prms = parameters as string ?? UrlUtils.ToUrlParamString(parameters);
            return new { refreshUrl = url, divId, prms, putToHistory };
            
        }

        /// <summary>
        /// Если при положительном ответе мы хотим частично обновить страницу
        /// </summary>
        /// <param name="divId">Id DOM элемента</param>
        /// <param name="url">Url частичной страницы</param>
        /// <param name="putToHistory">Попадает ли результат запроса в браузер</param>
        /// <returns>Ответ</returns>
        public static object JsqonOkRefershPartly(string divId, string url, bool putToHistory = false)
        {
            return new { refreshUrl = url, divId, putToHistory };

        }


        public static object PressBackButton()
        {
            return new { Status = "Back"}; 
        }

        /// <summary>
        /// Если при положительном ответе необходимо показать диалоговое окно
        /// </summary>
        /// <param name="dialogUrl">url на диалоговое окно</param>
        /// <param name="prms">Параметры</param>
        /// <returns>JSON</returns>
        public static object JsonOkAndShowDialog(string dialogUrl, object prms = null)
        {

            if (prms == null)
                return new { Status = "OkAndConfirm", DialogUrl = dialogUrl };

            return new { Status = "OkAndConfirm", DialogUrl = dialogUrl, Params = prms.ToUrlParamString() };
        }




        /// <summary>
        /// В случае отрицательного ответа, показываем ошибку, и указываем на дом элемент, у которого хотим показать ошибку
        /// </summary>
        /// <param name="result">Текстовый ответ</param>
        /// <param name="divError">Дом элемент, на котором показываем ошибку</param>
        /// <param name="popoverPosition">где распологается текст</param>
        /// <returns>Ответ</returns>
        public static object JsonFailResult(string result, string divError, PopoverPosition popoverPosition = PopoverPosition.Left)
        {if (popoverPosition == PopoverPosition.Left)
            return new { Status = "Fail", Result = result, divError };

        return new { Status = "Fail", Result = result, divError, Placement = popoverPosition.ToString().ToLower() };
        }


        public static object ShowDialogResult(string url, object @params = null)
        {
            if (@params == null)
                return new { status = "ShowDialog", url};

            return new { status = "ShowDialog", url, prms = @params };
        }

        public static object JsonFailResult(CheckDataException exception)
        {
            return JsonFailResult(exception.Message, exception.DivId, exception.PopoverPosition);
        }


        public static object JsonFailResult(ModelResult modelResult)
        {
            return JsonFailResult(modelResult.Text, modelResult.DivId, modelResult.Position);
        }

        public static object FillInput(string inputId, string text)
        {
            return new { inputId, text };
        }
    }
}
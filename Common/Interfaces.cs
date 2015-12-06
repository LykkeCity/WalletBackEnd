using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    /// <summary>
    /// Необходимо реализовать всем классам, у которых необходимо пропихивать какие то события через таймер.
    /// Например какой нибудь лист необходимо периодически чистить. Можно унаследовать у него интерфейс и тогда вызывать метод у него
    /// </summary>
    public interface ITimerable
    {
        /// <summary>
        /// 
        /// </summary>
        void Timer();
    }

    /// <summary>
    /// Награждаем данным интерфейсом все классы, которые перед нормальным запуском должны пройти самотестировани
    /// </summary>
    public interface ISelfTest
    {
        void SelfTest();
    }


    /// <summary>
    /// Награждаем все классы, которые после решистрации в IoC контейнере необходимо проинициализировать
    /// </summary>
    public interface IInitable
    {
        void BeforeInit();
        void Init();
        void AfterInit();

    }


    public interface IService
    {
        void Start();
        void Stop();
    }
}

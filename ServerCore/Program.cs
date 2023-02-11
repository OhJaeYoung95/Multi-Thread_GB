using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        #region Create Thread
        static void MainThread(object state)
        {
            for (int i = 0; i < 5; i++)
                Console.WriteLine("Hello Thread!");
        }       
        static void CreateThread()
        {
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(5, 5);

            for (int i = 0; i < 5; i++)
            {
                Task t = new Task(() => { while (true) { } }, TaskCreationOptions.LongRunning);
                t.Start();
            }

            //for (int i = 0; i < 4; i++)
            //    ThreadPool.QueueUserWorkItem((obj) => { while (true) { } });

            ThreadPool.QueueUserWorkItem(MainThread);


            //for (int i = 0; i < 1000; i++)
            //{
            //    Thread t = new Thread(MainThread);
            //    t.IsBackground = true;
            //    t.Start();
            //}
            //t.Name = "Test Thread";

            //Console.WriteLine("Waiting for Thread!");
            //t.Join();
            //Console.WriteLine("Hello World!");
            while (true)
            {

            }
        }
        #endregion

        #region Compiler Optimization
        volatile static bool _stop = false;

        static void ThreadMain()
        {
            Console.WriteLine("쓰레드 시작!");

            while(_stop == false)
            {
                // 누군가가 stop 신호를 해주기를 기다린다
            }

            Console.WriteLine("쓰레드 종료!");
        }

        static void CompilerOptimization()
        {
            Task t = new Task(ThreadMain);
            t.Start();

            Thread.Sleep(1000);

            _stop = true;

            Console.WriteLine("Stop 호출");
            Console.WriteLine("종료 대기중");

            t.Wait();

            Console.WriteLine("종료 성공");

        }
        static void Main(string[] args)
        {

        }
    }
}

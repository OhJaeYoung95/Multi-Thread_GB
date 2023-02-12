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
        #endregion

        #region Cache
        static void Cache()
        {
            int[,] arr = new int[10000, 10000];
            {
                long now = DateTime.Now.Ticks;
                for (int y = 0; y < 10000; y++)
                    for (int x = 0; x < 10000; x++)
                        arr[y, x] = 1;
                long end = DateTime.Now.Ticks;
                Console.WriteLine($"(y, x) 순서 걸린 시간 {end - now}");
            }
            {
                long now = DateTime.Now.Ticks;
                for (int y = 0; y < 10000; y++)
                    for (int x = 0; x < 10000; x++)
                        arr[x, y] = 1;
                long end = DateTime.Now.Ticks;
                Console.WriteLine($"(y, x) 순서 걸린 시간 {end - now}");
            }
        }
        #endregion

        #region Memory Barrier
        // 메모리 베리어
        // A) 코드 재배치 억제
        // B) 가시성
        //                        어셈블리언어                            값을 저장/ 값을 끄집어내는것
        // 1) Full Memory Barrier (ASM MFENCE, C# Thread.MemoryBarrier) : Store / Load 둘다 막는다
        // 2) Store Memeory Barrier (ASM SFENCE) : Store만 막는다
        // 3) Load Memeory Barrier (ASM LFENCE) : Load만 막는다
        static int x = 0;
        static int y = 0;
        static int r1 = 0;
        static int r2 = 0;
        static void MemoryBarrier1()
        {
            int count = 0;
            while (true)
            {
                count++;
                x = y = r1 = r2 = 0;

                Task t1 = new Task(Thread_1);
                Task t2 = new Task(Thread_2);
                t1.Start();
                t2.Start();

                Task.WaitAll(t1, t2);

                if (r1 == 0 && r2 == 0)
                    break;
            }

            Console.WriteLine($"{count}번 만에 빠져나옴!");
        }

        static void Thread_1()
        {
            y = 1; // Store y

            // ----------------------------
            Thread.MemoryBarrier();
            
            r1 = x; // Load x
        }

        static void Thread_2()
        {
            x = 1;  // Store x

            // ----------------------------
            Thread.MemoryBarrier();

            r2 = y; // Load y
        }

        static int _answer;
        static bool _complete;

        static void MemoryBarrier2()
        {
            Task t1 = new Task(A);
            Task t2 = new Task(B);

            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);
        }
        static void A()
        {
            _answer = 123;
            Thread.MemoryBarrier();
            _complete = true;
            Thread.MemoryBarrier();
        }
        static void B()
        {
            Thread.MemoryBarrier();
            if (_complete)
            {
                Thread.MemoryBarrier();
                Console.WriteLine(_answer);
            }
        }

        #endregion

        #region InterLocked
        // atomic = 원자성
        static volatile int number = 0;
        static void InterLockedThread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                // 1) 원자단위로 한번에 일어남 / All or Nothing(실행되거나 안되거나)
                Interlocked.Increment(ref number);
                // 2) number++;

                // 3) int temp = number;
                //    temp += 1;
                //    number = temp;
            }
        }
        static void InterLockedThread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                // 1) 원자단위로 한번에 일어남 / All or Nothing(실행되거나 안되거나)
                Interlocked.Decrement(ref number);
                // 2) number--;

                // 3) int temp = number;
                //    temp -= 1;
                //    number = temp;
            }
        }

        // Race Condition(경합조건) 해결 => Interlocked.
        static void InterLocked()
        {
            Task t1 = new Task(InterLockedThread_1);
            Task t2 = new Task(InterLockedThread_2);
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);

            Console.WriteLine(number);
        }
        #endregion

        #region Lock

        static  int locknumber = 0;
        static object _lockobj = new object();
        static void LockThread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                // 상호배제 Mutual Exclusive
                // CriticalSection(임계영역) C++ => std::mutex
                lock(_lockobj)
                {
                    locknumber++;
                }

                //Monitor.Enter(_lockobj);    // 문을 잠구는 행위, 들어와있다는 뜻(다른 쓰레드가 진입 불가)

                //locknumber++;

                //Monitor.Exit(_lockobj);     // 잠금을 풀어준다, 나갔다는 뜻(다른 쓰레드가 진입 가능)
            }
        }
        
        // 데드락 DeadLock
        static void LockThread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                // 상호배제 Mutual Exclusive
                lock(_lockobj)
                {
                    locknumber--;
                }

                //Monitor.Enter(_lockobj);    // 문을 잠구는 행위, 들어와있다는 뜻(다른 쓰레드가 진입 불가)

                //locknumber--;
                    
                //Monitor.Exit(_lockobj);     // 잠금을 풀어준다, 나갔다는 뜻(다른 쓰레드가 진입 가능)
            }
        }

        // Race Condition(경합조건) 해결 => Interlocked.
        static void Lock()
        {
            Task t1 = new Task(LockThread_1);
            Task t2 = new Task(LockThread_2);
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);

            Console.WriteLine(locknumber);
        }


        #endregion
        static void Main(string[] args)
        {
            Lock();
        }
    }
}

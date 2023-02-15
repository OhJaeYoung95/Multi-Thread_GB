using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    #region SpinLock Class
    class SpinLock
    {
        volatile int _locked = 0;
        public void Acquire()
        {
            //// 예시 1
            //while(true)
            //{
            //    // 원래의 값을 대입 시켜주고 값을 세팅해준다
            //    int original = Interlocked.Exchange(ref _locked, 1);
            //    if (original == 0)
            //        break;
            //}

            // 예시 2
            // CAS Compare-And-Swap
            while(true)
            {
                int expected = 0;
                int desired = 1;
                if( (Interlocked.CompareExchange(ref _locked, desired, expected)) == expected )
                    break;

                // 쉬다 올게~
                // Thread.Sleep(1);    // 무조건 휴식 => 무조건 1ms 정도 쉰다(시간은 운영체제가 대략적으로 비슷하도록 결정해줌)
                // Thread.Sleep(0);    // 조건부 양보 => 나보다 우선순위가 낮은 애들한테는 양보 불가
                                    // => 우선순위가 나보다 높거나 같은 쓰레드가 없으면 다시 본인한테옴
                Thread.Yield();     // 관대한 양보 => 관대하게 양보할테니, 지금 실행이 가능한 쓰레드가 있으면 실행
                                    // => 실행 가능한 애가 없으면 남은 시간 본인에게 소진
            }

            // 잘못된 예시
            //while (_locked)
            //{
            //    // 잠김이 풀리기를 기다린다
            //}
            //// 내꺼!
            //_locked = true;
        }

        public void Release()
        {
            _locked = 0;

            // 잘못된 예시
            //_locked = false;
        }
    }
    #endregion

    #region AutoResetEventClass

    class Lock
    {
        // bool <= 커널, 운영체제에게 부탁하는 방식(한번만 실행하더라도 부담이 된다.)
        AutoResetEvent _available = new AutoResetEvent(true);
        ManualResetEvent _manualavailable = new ManualResetEvent(true);

        public void Acquire()
        {
            _available.WaitOne();   // 입장 시도, AutoResetEvent에서는 입장후 문을 닫는것 까지 행한다
            // _availabe.Reset();   // bool = false, WaitOne()에 포함되어 있다.
        }
        public void ManualAcquire()
        {
            _manualavailable.WaitOne();   // 입장 시도, ManualResetEvent에서는 입장후 문을 닫지 않는다.
            _manualavailable.Reset();     // 이런식으로 입장을 통제해도 입장과 원자단위가 아니기에
                                          // 멀티쓰레드에서는 동시입장이 된다.
        }

        public void Release()
        {
            _available.Set();       // flag = true
        }
        public void ManualRelease()
        {
            _manualavailable.Set();       // flag = true
        }
    }

    #endregion

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

        #region SpinLock

        static int _spinLockNum = 0;
        static SpinLock _lock = new SpinLock();
        static void SpinLockThread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.Acquire();
                _spinLockNum++;
                _lock.Release();
            }
        }
        static void SpinLockThread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.Acquire();
                _spinLockNum--;
                _lock.Release();
            }

        }

        static void TestSpinLock()
        {
            Task t1 = new Task(SpinLockThread_1);
            Task t2 = new Task(SpinLockThread_2);
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);

            Console.WriteLine(_spinLockNum);
        }
        #endregion

        #region AutoResetEvent

        static int _autoResetEventLockNum = 0;
        static Lock _autoResetEventlock = new Lock();
        static void AutoResetEventLockThread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                _autoResetEventlock.Acquire();
                _autoResetEventLockNum++;
                _autoResetEventlock.Release();
            }
        }
        static void AutoResetEventLockThread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                _autoResetEventlock.Acquire();
                _autoResetEventLockNum--;
                _autoResetEventlock.Release();
            }

        }

        static void TestAutoResetEventLock()
        {
            Task t1 = new Task(AutoResetEventLockThread_1);
            Task t2 = new Task(AutoResetEventLockThread_2);
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);

            Console.WriteLine(_autoResetEventLockNum);
        }


        #endregion


        static void Main(string[] args)
        {
            TestAutoResetEventLock();
        }
    }
}

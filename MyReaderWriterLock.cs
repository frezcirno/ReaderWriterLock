// Author : frezcirno (2277861660@qq.com)
// Since  : 2021.5.4

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ReaderWriterLock
{
    /// <summary>
    /// 写者优先的读写锁
    /// </summary>
    public class MyReaderWriterLock
    {
        private int _readerNumber; // 读者数量
        private int _writerWaitingNumber; // 正在等待的写者数量
        private bool _isWriting; // 是否正在写
        private readonly object _rwLock = new object(); // 读写互斥量

        /// <summary> 
        /// 获取读锁
        /// </summary>
        public void AcquireReaderLock()
        {
            lock (_rwLock)
            {
                // 如果当前有写线程正在写（读写互斥）
                // 或者有写线程正在等待（写者优先）
                // 就挂起当前线程等待
                while (_isWriting || _writerWaitingNumber != 0)
                {
                    Monitor.Wait(_rwLock);
                }

                // 读者数量++
                _readerNumber++;
            }
        }

        /// <summary>
        /// 释放读锁
        /// </summary>
        public void ReleaseReaderLock()
        {
            lock (_rwLock)
            {
                // 读者数量--
                _readerNumber--;

                // 若所有读者线程都结束了，唤醒一个正在等待的写线程
                if (_readerNumber == 0 && _writerWaitingNumber > 0) {
                    Monitor.Pulse(_rwLock);
                }
            }
        }

        /// <summary>
        /// 获取写锁
        /// </summary>
        public void AcquireWriterLock()
        {
            lock (_rwLock)
            {
                // 标记有写线程正在等待
                _writerWaitingNumber++;

                // 如果当前有读线程正在读（读写互斥）
                // 或者有写线程正在写（写写互斥）
                // 就挂起当前线程等待
                while (_readerNumber != 0 || _isWriting)
                {
                    Monitor.Wait(_rwLock);
                }

                // 当前写线程拿到写锁
                // 正在等待写线程数--
                _writerWaitingNumber--;
                _isWriting = true;
            }
        }

        /// <summary>
        /// 释放写锁
        /// </summary>
        public void ReleaseWriterLock()
        {
            lock (_rwLock)
            {
                // 释放写锁，唤醒等待的线程
                _isWriting = false;
                Monitor.PulseAll(_rwLock);
            }
        }
    }


    /// <summary>
    /// 测试程序
    /// </summary>
    public class Program
    {
        private static readonly MyReaderWriterLock _lock = new MyReaderWriterLock();

        /// <summary>
        /// 读线程：获取读锁后等待 1s，再释放读锁
        /// </summary>
        private static void ReadValue()
        {
            _lock.AcquireReaderLock();
            Console.Out.WriteLine("Thread-" + Thread.CurrentThread.ManagedThreadId + " is Reading.");
            Thread.Sleep(1000);
            Console.Out.WriteLine("Thread-" + Thread.CurrentThread.ManagedThreadId + " read done.");
            _lock.ReleaseReaderLock();
        }


        /// <summary>
        /// 写线程：获取写锁后等待 2s，再释放写锁
        /// </summary>
        private static void WriteValue()
        {
            _lock.AcquireWriterLock();
            Console.Out.WriteLine("Thread-" + Thread.CurrentThread.ManagedThreadId + " is Writing.");
            Thread.Sleep(2000);
            _lock.ReleaseWriterLock();
        }


        public static void Main(string[] args)
        {
            List<Thread> threads = new List<Thread>();

            // 创建 6 个读线程
            for (int i = 0; i < 6; i++)
            {
                threads.Add(new Thread(ReadValue));
                threads.Last().Start();
            }

            // 创建 3 个写线程
            for (int i = 0; i < 3; i++)
            {
                threads.Add(new Thread(WriteValue));
                threads.Last().Start();
            }

            // 再创建 6 个读线程
            for (int i = 0; i < 6; i++)
            {
                threads.Add(new Thread(ReadValue));
                threads.Last().Start();
            }

            // 再创建 3 个写线程
            for (int i = 0; i < 3; i++)
            {
                threads.Add(new Thread(WriteValue));
                threads.Last().Start();
            }

            // 等待所有线程结束
            threads.ForEach(thread => thread.Join());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Drawing;
using System.Collections;

namespace Blastagon.Common
{
    // lockを利用時にかける必要のあるListを扱うためのクラス
    public class LockList<TValue> : IEnumerable<TValue>
    {
        const int LOCK_TIME = 1;

        private List<TValue> list;
        private object list_lock = new object();

        public LockList()
        {
            this.list = new List<TValue>();
        }

        public LockList(List<TValue> list)
        {
            this.list = list;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            //lock (list_lock)
            //{
            //}
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(list_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    yield return list[i];
                }
            }
            finally
            {
                Monitor.Exit(list_lock);
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Foreach(Action<TValue> action)
        {
            //lock (list_lock)
            //{
            //    foreach (var i in list)
            //    {
            //        action(i);
            //    }
            //}
            LockEnter(() =>
            {
                foreach (var i in list)
                {
                    action(i);
                }
            });
        }

        // actionの戻り値がtrueだと、foreachが中断される
        public void ForeachBreak(Func<TValue, bool> action)
        {
            //lock (list_lock)
            //{
            //    foreach (var i in list)
            //    {
            //        if (action(i)) break;
            //    }
            //}
            LockEnter(() =>
            {
                foreach (var i in list)
                {
                    if (action(i)) break;
                }
            });
        }

        public int Count()
        {
            //lock (list_lock)
            //{
            //    return list.Count;
            //}
            var count = 0;
            LockEnter(() =>
            {
                count = list.Count;
            });
            return count;
        }

        public void Add( TValue value)
        {
            //lock (list_lock)
            //{
            //    list.Add( value);
            //}
            LockEnter(() =>
            {
                list.Add(value);
            });
        }

        public void Remove(TValue value)
        {
            //lock (list_lock)
            //{
            //    list.Remove(value);
            //}
            LockEnter(() =>
            {
                list.Remove(value);
            });
        }

        public void RemoveAt(int index)
        {
            //lock (list_lock)
            //{
            //    list.RemoveAt(index);
            //}
            LockEnter(() =>
            {
                list.RemoveAt(index);
            });
        }

        public int IndexOf( TValue value)
        {
            //lock (list_lock)
            //{
            //    return list.IndexOf(value);
            //}
            var res = 0;
            LockEnter(() =>
            {
                res = list.IndexOf(value);
            });
            return res;
        }

        public void Insert( int index, TValue value)
        {
            //lock (list_lock)
            //{
            //    list.Insert( index, value);
            //}
            LockEnter(() =>
            {
                list.Insert(index, value);
            });
        }

        public void Clear()
        {
            //lock (list_lock)
            //{
            //    list.Clear();
            //}
            LockEnter(() =>
            {
                list.Clear();
            });
        }

        public TValue this[int index]
        {
            get
            {
                //lock (list_lock)
                //{
                //    return this.list[index];
                //}
                var is_enter = false;
                try
                {
                    while (!is_enter)
                    {
                        Monitor.TryEnter(list_lock, LOCK_TIME, ref is_enter);
                        if (!is_enter)
                        {
                            WaitSleep.Do(0);
                        }
                    }

                    return this.list[index];
                }
                finally
                {
                    Monitor.Exit(list_lock);
                }
            }
        }

        public TValue PopFront()
        {
            //lock (list_lock)
            //{
            //}
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(list_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                var res = list[0];
                list.RemoveAt(0);
                return res;
            }
            finally
            {
                Monitor.Exit(list_lock);
            }
        }


        private void LockEnter(Action action)
        {
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(list_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                action();
            }
            finally
            {
                Monitor.Exit(list_lock);
            }
        }
    }

    // lockを利用時にかける必要のあるDictionaryを扱うためのクラス
    public class LockDictionary<TKey,TValue> : IEnumerable<TValue>
    {
        const int LOCK_TIME = 1;

        private Dictionary<TKey, TValue> dictionary;
        private object dictionary_lock = new object();

        public LockDictionary(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        public void Foreach(  Action<KeyValuePair<TKey, TValue>> action )
        {
            LockEnter(() =>
            {
                foreach (var i in dictionary)
                {
                    action(i);
                }
            });
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(dictionary_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                //for (var i = 0; i < dictionary.Count; i++)
                foreach (var i in dictionary )
                {
                    yield return i.Value;
                }
            }
            finally
            {
                Monitor.Exit(dictionary_lock);
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        // actionの戻り値がtrueだと、foreachが中断される
        public void ForeachBreak(Func<KeyValuePair<TKey, TValue>,bool> action)
        {
            LockEnter(() =>
            {
                foreach (var i in dictionary)
                {
                    if (action(i)) break;
                }
            });
        }

        public int Count()
        {
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(dictionary_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                return dictionary.Count;
            }
            finally
            {
                Monitor.Exit(dictionary_lock);
            }
        }

        public void Add(TKey key, TValue value)
        {
            LockEnter(() =>
            {
                dictionary.Add(key, value);
            });
        }

        public void Remove(TKey key)
        {

            LockEnter(() =>
            {
                dictionary.Remove(key);
            });
        }

        public bool ContainsKey(TKey key)
        {
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(dictionary_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                return dictionary.ContainsKey(key);
            }
            finally
            {
                Monitor.Exit(dictionary_lock);
            }
        }

        public TValue this[TKey key]
        {
            //set { this.dictionary[key] = value; }
            get {
                var is_enter = false;
                try
                {
                    while (!is_enter)
                    {
                        Monitor.TryEnter(dictionary_lock, LOCK_TIME, ref is_enter);
                        if (!is_enter)
                        {
                            WaitSleep.Do(0);
                        }
                    }

                    return this.dictionary[key];
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Monitor.Exit(dictionary_lock);
                }
            }
        }

        private void LockEnter(Action action)
        {
            var is_enter = false;
            try
            {
                while (!is_enter)
                {
                    Monitor.TryEnter(dictionary_lock, LOCK_TIME, ref is_enter);
                    if (!is_enter)
                    {
                        WaitSleep.Do(0);
                    }
                }

                action();
            }
            finally
            {
                Monitor.Exit(dictionary_lock);
            }
        }
    }


    public class Lock
    {
        const int LOCK_TIME = 1;
        object lock_object = new object();

        public void Enter()
        {
            var is_enter = false;
            while (!is_enter)
            {
                Monitor.TryEnter(lock_object, LOCK_TIME, ref is_enter);
                if (!is_enter)
                {
                    WaitSleep.Do(0);
                }
            }
        }

        public void Exit()
        {
            Monitor.Exit(lock_object);
        }
    }

    public class Hit
    {
        public static bool IsHit(int x1, int y1, Rectangle rect2)
        {
            if ((rect2.X <= x1 && x1 <= rect2.X + rect2.Width) && (
                   rect2.Y <= y1 && y1 <= rect2.Y + rect2.Height))
            {
                return true;
            }
            return false;
        }

        public static bool IsHit(int x1, int y1, RectangleD rect2)
        {
            if ((rect2.X <= x1 && x1 <= rect2.X + rect2.Width) && (
                   rect2.Y <= y1 && y1 <= rect2.Y + rect2.Height))
            {
                return true;
            }
            return false;
        }
    }

    // スレッドをフリーズ(停止)させないため
    // Taskを使ったスリープ
    // 処理速度・精度は落ちる
    public class WaitSleep
    {
        static public void Do(int time)
        {
            Task taskA = Task.Factory.StartNew(() => _Sleep_Task(time));
            taskA.Wait();
        }

        static private void _Sleep_Task(int time)
        {
            Thread.Sleep(time);
        }
    }

    public class StringBase64
    {
        const string enc_str = "UTF-8";

        public static string ToBase64(string str)
        {
            var enc = Encoding.GetEncoding(enc_str);
            var res = Convert.ToBase64String(enc.GetBytes(str));
            return res;
        }

        public static string ToUTF8(string str)
        {
            var enc = Encoding.GetEncoding(enc_str);
            var res = enc.GetString(Convert.FromBase64String(str));
            return res;
        }
    }

}

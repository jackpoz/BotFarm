using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    [Flags]
    public enum ActionFlag
    {
        None = 0x0,
        Movement = 0x1
    }

    public class RepeatingAction
    {
        public Action Action
        {
            get;
            private set;
        }

        public DateTime ScheduledTime
        {
            get;
            private set;
        }

        public TimeSpan Interval
        {
            get;
            private set;
        }

        public ActionFlag Flags
        {
            get;
            private set;
        }

        public int Id
        {
            get;
            private set;
        }

        public RepeatingAction(Action action, DateTime scheduledTime, TimeSpan interval, ActionFlag flags, int id)
        {
            this.Action = action;
            this.ScheduledTime = scheduledTime;
            this.Interval = interval;
            this.Flags = flags;
            this.Id = id;
        }
    }

    public class ScheduledActions : IList<RepeatingAction>
    {
        List<RepeatingAction> actions;

        public ScheduledActions()
        {
            actions = new List<RepeatingAction>();
        }

        public int IndexOf(RepeatingAction item)
        {
            return actions.IndexOf(item);
        }

        void IList<RepeatingAction>.Insert(int index, RepeatingAction item)
        {
            Add(item);
        }

        public void RemoveAt(int index)
        {
            actions.RemoveAt(index);
        }

        public RepeatingAction this[int index]
        {
            get
            {
                return actions[index];
            }
            set
            {
                actions[index] = value;
                Sort();
            }
        }

        public void Add(RepeatingAction item)
        {
            actions.Add(item);
            Sort();
        }

        public void Clear()
        {
            actions.Clear();
        }

        public bool Contains(RepeatingAction item)
        {
            return actions.Contains(item);
        }

        public void CopyTo(RepeatingAction[] array, int arrayIndex)
        {
            actions.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return actions.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(RepeatingAction item)
        {
            return actions.Remove(item);
        }

        public int RemoveByFlag(ActionFlag flag)
        {
            return actions.RemoveAll(action => action.Flags.HasFlag(flag));
        }

        public bool Remove(int actionId)
        {
            return actions.RemoveAll(action => action.Id == actionId) > 0;
        }

        public IEnumerator<RepeatingAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        void Sort()
        {
            actions.Sort((a, b) => (int)(a.ScheduledTime - b.ScheduledTime).TotalMilliseconds);
        }
    }
}

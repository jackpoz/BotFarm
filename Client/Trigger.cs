using Client.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Trigger
    {
        public List<TriggerAction> TriggerActions
        {
            get;
            private set;
        }

        public Action FinalAction
        {
            get;
            private set;
        }

        public Trigger(IEnumerable<TriggerAction> TriggerActions, Action FinalAction)
        {
            this.TriggerActions = TriggerActions.ToList();
            this.FinalAction = FinalAction;
        }

        public void HandleInput(TriggerActionType type, params object[] inputs)
        {
            foreach (var trigger in TriggerActions)
            {
                if (trigger.Triggered)
                    continue;

                if (trigger.IsTriggered(type, inputs))
                    break;
            }

            if (TriggerActions.Last().Triggered)
            {
                if (FinalAction != null)
                    FinalAction();
                Reset();
            }
        }

        public void Reset()
        {
            TriggerActions.ForEach(trigger => trigger.Reset());
        }
    }

    public class OpcodeTriggerAction : TriggerAction
    {
        public WorldCommand Opcode
        {
            get;
            private set;
        }

        public OpcodeTriggerAction(WorldCommand Opcode)
            : base(TriggerActionType.Opcode)
        {
            this.Opcode = Opcode;
        }

        protected override bool CheckIfTriggered(TriggerActionType type, params object[] inputs)
        {
            if (type != Type)
                return false;

            if (inputs == null || inputs.Length == 0 || inputs[0] == null || inputs[0].GetType() != typeof(WorldCommand))
                return false;

            if ((WorldCommand)inputs[0] != Opcode)
                return false;

            return true;
        }
    }

    public abstract class TriggerAction
    {
        public TriggerActionType Type
        {
            get;
            private set;
        }

        public bool Triggered
        {
            get;
            set;
        }

        public Action IntermediateAction
        {
            get;
            private set;
        }

        public TriggerAction(TriggerActionType Type, Action IntermediateAction = null)
        {
            this.Type = Type;
            this.IntermediateAction = IntermediateAction;
        }

        public void Reset()
        {
            Triggered = false;
        }

        public bool IsTriggered(TriggerActionType type, params object[] inputs)
        {
            if (CheckIfTriggered(type, inputs))
            {
                Triggered = true;
                if (IntermediateAction != null)
                    IntermediateAction();
                return true;
            }
            else
                return false;
        }

        protected abstract bool CheckIfTriggered(TriggerActionType type, params object[] inputs);
    }

    public enum TriggerActionType
    {
        None,
        Opcode
    }
}

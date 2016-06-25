using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.AI
{
    public interface ICombatAI : IGameAI
    {
    }

    public class EmptyCombatAI : ICombatAI
    {
        public bool Activate(AutomatedGame game)
        {
            return true;
        }

        public void Deactivate()
        { }

        public void Pause()
        { }

        public void Resume()
        { }

        public void Update()
        { }

        public bool AllowPause()
        {
            return true;
        }
    }
}

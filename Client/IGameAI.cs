using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface IGameAI
    {
        void Activate();
        void Deactivate();
        void Pause();
        void Resume();
        void Update(AutomatedGame game);
        bool AllowPause();
    }

    public class EmptyAI : IGameAI
    {
        public void Activate()
        { }

        public void Deactivate()
        { }

        public void Pause()
        { }

        public void Resume()
        { }

        public void Update(AutomatedGame game)
        { }

        public bool AllowPause()
        {
            return true;
        }
    }
}

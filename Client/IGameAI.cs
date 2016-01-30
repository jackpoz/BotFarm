using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface IGameAI
    {
        void Activate(AutomatedGame game);
        void Deactivate();
        void Pause();
        void Resume();
        void Update();
        bool AllowPause();
    }

    public class EmptyAI : IGameAI
    {
        public void Activate(AutomatedGame game)
        { }

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

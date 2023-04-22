using System;
using UnityEngine;

namespace VentiCola.UI
{
    public interface IViewController
    {
        ref readonly UIConfig Config { get; }

        UIState State { get; }

        int StackIndex { get; }

        GameObject ViewInstance { get; }

        event Action<IViewController> OnViewChanged;

        event Action<IViewController> OnClosingCompleted;

        void InitView(GameObject viewInstance);

        void Open(int stackIndex);

        void Pause();

        void Resume();

        void Close();

        void Update();

        void LateUpdate();
    }
}
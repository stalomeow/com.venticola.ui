using System;
using UnityEngine;

namespace VentiColaEditor.UI.CodeInjection
{
    [Flags]
    public enum InjectionTasks
    {
        [InspectorName("Nothing")]
        None = 0,

        [InspectorName("Reactive/Auto-Property")]
        Reactive_AutoProperty = 1 << 0,

        [InspectorName("Reactive/Lazy-Computed")]
        Reactive_LazyComputed = 1 << 1,

        [InspectorName("Reactive/Optimize dynamic reading")]
        Reactive_ModelGet = 1 << 2,

        [InspectorName("Reactive/Optimize dynamic writing")]
        Reactive_ModelSet = 1 << 3,

        [InspectorName("Optimize Dynamic Invocations/Callback")]
        UIPage_Callback = 1 << 4,

        [InspectorName("Optimize Dynamic Invocations/Event handler")]
        UIPage_EventHandler = 1 << 5,

        [InspectorName("Everything")]
        All = Reactive_AutoProperty | Reactive_LazyComputed | Reactive_ModelGet | Reactive_ModelSet | UIPage_Callback | UIPage_EventHandler
    }
}
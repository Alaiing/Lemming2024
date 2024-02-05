using System;
using System.Collections.Generic;

namespace Oudidon
{
    public static class EventsManager
    {
        public class Event
        {
            public string name;
            protected event Action Actions;

            public void FireEvent()
            {
                Actions?.Invoke();
            }

            public void AddListener(Action action)
            {
                Actions += action;
            }

            public void RemoveListener(Action action)
            {
                Actions -= action;
            }
        }
        public class Event<T> : Event
        {
            protected new event Action<T> Actions;

            public void FireEvent(T param)
            {
                Actions?.Invoke(param);
            }

            public void AddListener(Action<T> action)
            {
                Actions += action;
            }

            public void RemoveListener(Action<T> action)
            {
                Actions -= action;
            }
        }

        public class Event<T, Y> : Event
        {
            protected new event Action<T, Y> Actions;

            public void FireEvent(T param1, Y param2)
            {
                Actions?.Invoke(param1, param2);
            }

            public void AddListener(Action<T, Y> action)
            {
                Actions += action;
            }

            public void RemoveListener(Action<T, Y> action)
            {
                Actions -= action;
            }
        }

        private static Dictionary<string, Event> _basicEvents = new Dictionary<string, Event>();

        public static void ListenTo(string eventName, Action action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value))
            {
                value.AddListener(action);
            }
            else
            {
                Event newEvent = new Event() { name = eventName };
                newEvent.AddListener(action);
                _basicEvents.Add(eventName, newEvent);
            }
        }

        public static void StopListening(string eventName, Action action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value))
            {
                value.RemoveListener(action);
            }
        }

        public static void ListenTo<T>(string eventName, Action<T> action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T> oneParamEvent)
            {
                oneParamEvent.AddListener(action);
            }
            else
            {
                Event<T> newEvent = new Event<T>() { name = eventName };
                newEvent.AddListener(action);
                _basicEvents.Add(eventName, newEvent);
            }
        }

        public static void StopListening<T>(string eventName, Action<T> action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T> oneParamEvent)
            {
                oneParamEvent.RemoveListener(action);
            }
        }

        public static void ListenTo<T, Y>(string eventName, Action<T, Y> action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T, Y> oneParamEvent)
            {
                oneParamEvent.AddListener(action);
            }
            else
            {
                Event<T, Y> newEvent = new Event<T, Y>() { name = eventName };
                newEvent.AddListener(action);
                _basicEvents.Add(eventName, newEvent);
            }
        }

        public static void StopListening<T, Y>(string eventName, Action<T, Y> action)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T, Y> twoParamEvent)
            {
                twoParamEvent.RemoveListener(action);
            }
        }

        public static void FireEvent(string eventName)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value))
            {
                value.FireEvent();
            }
        }

        public static void FireEvent<T>(string eventName, T param)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T> oneParamEvent)
            {
                oneParamEvent.FireEvent(param);
            }
        }

        public static void FireEvent<T, Y>(string eventName, T param1, Y param2)
        {
            if (_basicEvents.TryGetValue(eventName, out Event value) && value is Event<T, Y> twoParamEvent)
            {
                twoParamEvent.FireEvent(param1, param2);
            }
        }
    }
}

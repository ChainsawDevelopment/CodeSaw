﻿namespace CodeSaw.Web.Cqrs
{
    public interface IEventBus
    {
        void Publish(Event @event);
    }
}
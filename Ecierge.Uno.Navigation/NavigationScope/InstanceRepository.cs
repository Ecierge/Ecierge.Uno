namespace Ecierge.Uno;

using System;
using System.Collections.Generic;

internal interface IInstanceRepository
{
    IDictionary<Type, object> Instances { get; }
}

public class InstanceRepository : IInstanceRepository, ISingletonInstanceRepository, IScopedInstanceRepository
{
    public IDictionary<Type, object> Instances { get; } = new Dictionary<Type, object>();
}

internal interface IScopedInstanceRepository : IInstanceRepository { }

internal interface ISingletonInstanceRepository : IInstanceRepository { }

public class ScopedInstanceRepositoryOptions
{
    public HashSet<Type> TypesToClone { get; } = new HashSet<Type>();

    public ScopedInstanceRepositoryOptions AddTypeToClone(Type type)
    {
        TypesToClone.Add(type);
        return this;
    }

    public ScopedInstanceRepositoryOptions AddTypeToClone<T>() => AddTypeToClone(typeof(T));
}

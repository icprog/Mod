﻿using Mod.Configuration.Properties;
using Mod.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Mod.Interfaces.Pipes;
using Mod.Modules.Abstracts;
using Mod.Configuration.Modules;
using Mod.Interfaces.Containers;

namespace Mod.Modules.EndPoints
{
  [Configure(InitType = typeof(Pipe<IJob>))]
  public class Pipe<T>: Lockable, IPipe<T>
  {
    private IList<T> data = null;
    private IObjectContainer basePipe = null;

    [Configure(InitType = typeof(List<>))]
    public virtual IList<T> Data
    {
      get
      {
        lock(Padlock) return data;
      }
      set
      {
        lock(Padlock) data = value;
      }
    }

    public virtual T Pop()
    {      
      lock(Padlock)
      {
        T result = default(T);
        if (basePipe != this)
          result = (T)basePipe.PopObject();
        else
        {
          result = default(T);
          if (data.Count > 0)
          {
            result = data.First();
            data.Remove(result);
          }
        }
        return result;
      }
    }

    public virtual bool Push(T element)
    {
      lock (Padlock)
      {
        if (basePipe != this)
          return basePipe.PushObject(element);        
        else        
          Data.Add(element);        
      }     
      return true;
    }

    public virtual ModuleConfig ToConfig()
    {
      throw new NotImplementedException();
    }

    public virtual bool FromConfig(ModuleConfig config)
    {
      throw new NotImplementedException();
    }

    public override bool Initialize()
    {
      if (base.Initialize())
      {
        if(basePipe == null)
          basePipe = this;
        return true;
      }
      return false;
    }

    public virtual IEnumerator<T> GetEnumerator()
    {
      lock(Padlock) return Data.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return Data.GetEnumerator();
    }

    public virtual int IndexOf(T item)
    {
      lock(Padlock) return Data.IndexOf(item);
    }

    public virtual void Insert(int index, T item)
    {
      lock(Padlock) Data.Insert(index, item);
    }

    public virtual void RemoveAt(int index)
    {
      lock(Padlock) Data.RemoveAt(index);
    }

    public virtual T this[int index]
    {
      get
      {
        lock(Padlock) return Data[index];
      }
      set
      {
        lock(Padlock) Data[index] = value;
      }
    }

    public virtual void Add(T item)
    {
      Push(item);
    }

    public virtual void Clear()
    {
      lock(Padlock) Data.Clear();
    }

    public virtual bool Contains(T item)
    {
      return Contains(item);
    }

    public virtual void CopyTo(T[] array, int arrayIndex)
    {
      lock(Padlock) Data.CopyTo(array, arrayIndex);
    }

    public virtual int Count
    {
      get { lock(Padlock) return Data.Count; }
    }

    public virtual bool IsReadOnly
    {
      get { lock(Padlock) return Data.IsReadOnly; }
    }

    public virtual bool Remove(T item)
    {
      lock(Padlock) return Data.Remove(item);
    }

    public virtual object PopObject()
    {
      return Pop();
    }

    public virtual bool PushObject(object element)
    {
      return Push((T)element);
    }

    [Configure()]
    public virtual IObjectContainer BasePipe
    {
      get { lock (Padlock) return basePipe; }
      set { lock(Padlock) basePipe = value; }
    }
  }
}

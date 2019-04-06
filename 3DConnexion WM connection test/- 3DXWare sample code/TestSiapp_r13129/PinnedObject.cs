///////////////////////////////////////////////////////////////////////////////////
// Copyright notice:
//   Copyright (c) 2016 3Dconnexion. All rights reserved.
//
// This file and source code are an integral part of the "3Dconnexion
// Software Development Kit", including all accompanying documentation,
// and is protected by intellectual property laws. All use of the
// 3Dconnexion Software Development Kit is subject to the License
// Agreement found in the "LicenseAgreementSDK.txt" file.
// All rights not expressly granted by 3Dconnexion are reserved.
//

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace _3Dconnexion
{
  /// <summary>
  /// Helper class for marshaling
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class PinnedObject<T> : IDisposable
  {
    #region member variables

    private T t;
    private GCHandle handle;

    #endregion member variables

    #region properties

    public T Value
    {
      get
      {
        return t;
      }
      set
      {
        t = value;
      }
    }

    #endregion properties

    public bool IsPinned
    {
      get
      {
        return handle.IsAllocated;
      }
    }

    public IntPtr PinObject()
    {
      if (disposedValue)
        throw new ObjectDisposedException(this.ToString());

      if (!handle.IsAllocated)
        handle = GCHandle.Alloc(t, GCHandleType.Pinned);

      return handle.AddrOfPinnedObject();
    }

    public IntPtr UnpinObject()
    {
      if (handle.IsAllocated)
        handle.Free();

      return IntPtr.Zero;
    }

    #region IDisposable Support

    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // dispose managed state (managed objects).
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.
        UnpinObject();

        disposedValue = true;
      }
    }

    // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~PinnedObject()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      //  SuppressFinalize if the finalizer is overridden above.
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
  }

  public class UTF8String : PinnedObject<byte[]>
  {
    #region member variables

    private string _s;

    #endregion member variables

    public new string Value
    {
      get
      {
        return _s;
      }
      set
      {
        _s = value;
      }
    }

    public new IntPtr PinObject()
    {
      base.Value = Encoding.UTF8.GetBytes(_s + '\0');

      return base.PinObject();
    }

    #region IDisposable Support

    private bool disposedValue = false; // To detect redundant calls

    protected override void Dispose(bool disposing)
    {
      if (disposedValue)
        return;

      if (disposing)
      {
        // dispose managed state (managed objects).
      }

      disposedValue = true;

      base.Dispose(disposing);
    }

    #endregion IDisposable Support
  }
}
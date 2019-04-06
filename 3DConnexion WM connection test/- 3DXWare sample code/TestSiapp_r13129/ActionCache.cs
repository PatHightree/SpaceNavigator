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
using System.Collections.Generic;

namespace _3Dconnexion
{
  /// <summary>
  /// ActionCache represents the actions/commands and images of the 3Dxware client application.
  /// The branches (categories) of the ActionTree represent menus / submenus
  /// The leafs (actions) represent commands that can be assigned to 3Dconnexion device buttons.
  /// An actionset represents a context sensitive set of categories/commands
  /// </summary>
  public class ActionCache : IDisposable
  {
    private class ActionTree : ActionNode
    {
      public new IntPtr PinObject()
      {
        if (Count > 0)
          return base[0].PinObject();

        return IntPtr.Zero;
      }

      public new IntPtr UnpinObject()
      {
        if (Count > 0)
          return base[0].UnpinObject();

        return IntPtr.Zero;
      }
    }

    #region member variables

    private ActionTree m_tree = new ActionTree();
    private List<ImageItem> m_images = new List<ImageItem>();

    #endregion member variables

    public ActionCache()
    {
    }

    #region properties

    public ImageItem ImageItem(int index)
    {
      return m_images[index];
    }

    public int ImageCount
    {
      get
      {
        return m_images.Count;
      }
    }

    public ActionNode ActionItem(int index)
    {
      return m_tree[index];
    }

    public int ActionCount
    {
      get
      {
        return m_tree.Count;
      }
    }

    #endregion properties

    /// <summary>
    /// Add the root of a command/action set
    /// action sets can be thought of as menu- or toolbars
    /// </summary>
    /// <param name="actionSet"></param>
    /// <returns></returns>
    public ActionNode Add(ActionSet actionSet)
    {
      return m_tree.Add(actionSet);
    }

    public void Add(ImageItem item)
    {
      m_images.Add(item);
    }

    /// <summary>
    /// Saves the complete commands and images to the 3dxware UI cache
    /// </summary>
    /// <param name="hdl">The value returned from SiOpen</param>
    public void SaveTo3Dxware(IntPtr hdl)
    {
      SaveImagesTo3Dxware(hdl);

      SaveActionsTo3Dxware(hdl);
    }

    /// <summary>
    /// Saves the commands to the 3dxware UI cache
    /// </summary>
    /// <param name="hdl">The value returned from SiOpen</param>
    public void SaveActionsTo3Dxware(IntPtr hdl)
    {
      SiApp.SpwRetVal result;
      try
      {
        result = SiApp.SiAppCmdWriteActions(hdl, m_tree.PinObject());
      }
      catch (Exception e)
      {
        throw e;
      }
      finally
      {
        m_tree.UnpinObject();
      }

      if (result != SiApp.SpwRetVal.SPW_NO_ERROR)
        throw new System.IO.IOException("Cannot write actions to 3Dconnexion Device", (int)result);
    }

    /// <summary>
    /// Save all image items to the 3Dxware UI cache
    /// </summary>
    /// <param name="hdl">The value returned from SiOpen</param>
    public void SaveImagesTo3Dxware(IntPtr hdl)
    {
      // Temporary list for the SiImage_s structures
      List<SiApp.SiImage_s> images = new List<SiApp.SiImage_s>();

      m_images.ForEach((item) => images.Add(item.PinObject()));

      // The pin visitor pins the managed image item class so that it can be marshaled to the 3dxware unmanaged
      // code and adds the pinned object to the image list
      Action<ActionNode> pinImage = (node) => { if (node.Image != null) images.Add(node.Image.PinObject()); };

      // The unpin visitor frees unmanaged memory
      Action<ActionNode> unpinImage = (node) => { if (node.Image != null) node.Image.UnpinObject(); };

      // Pin image objects and fill list
      m_tree.ForEach(pinImage);
      SiApp.SpwRetVal result;
      try
      {
        // Save the images to the 3Dxware UI cache
        result = SiApp.SiAppCmdWriteActionImages(hdl, images.ToArray(), images.Count);
      }
      catch (Exception e)
      {
        throw e;
      }
      finally
      {
        // Unpin
        m_images.ForEach((item) => { item.UnpinObject(); });
        m_tree.ForEach(unpinImage);
      }

      // clear the list
      images.Clear();

      if (result != SiApp.SpwRetVal.SPW_NO_ERROR)
        throw new System.IO.IOException("Cannot write images to 3Dconnexion Device", (int)result);
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
          m_tree.Dispose();
          m_images.Clear();
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.

        disposedValue = true;
      }
    }

    // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~ActionCache() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    void IDisposable.Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
  }
}
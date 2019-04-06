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
using System.Drawing;
using System.Runtime.InteropServices;

namespace _3Dconnexion
{
  public abstract class ImageItem : IDisposable
  {
    #region member variables

    protected SiApp.SiImage_s m_siImage;
    private UTF8String m_Id = new UTF8String();

    #endregion member variables

    public string Id
    {
      get
      {
        return m_Id.Value;
      }
      set
      {
        if (value.Length == 0)
          throw new ArgumentException("Invalid Item Id");
        m_Id.Value = value;
      }
    }

    protected ImageItem(string id = "")
    {
      m_siImage.size = Marshal.SizeOf(typeof(SiApp.SiImage_s));
      m_siImage.type = SiApp.SiImageType_t.e_none;
      if (id.Length == 0)
        Id = @"id_" + Guid.NewGuid().ToString();
      else
        Id = id;
    }

    public static ImageItem FromFile(string filename, int index = 0, string id = "")
    {
      ImageItem item = new ImageFile(filename, index, id);
      return item;
    }

    public static ImageItem FromResource(string resourceDllPath, string resourceId, string resourceType, int index = 0, string id = "")
    {
      ImageItem item = new ImageResource(resourceDllPath, resourceId, resourceType, index, id);
      return item;
    }

    public static ImageItem FromImage(Image image, int index = 0, string id = "")
    {
      ImageItem item = new ImageData(image, index, id);
      return item;
    }

    public virtual SiApp.SiImage_s PinObject()
    {
      m_siImage.id = m_Id.PinObject();
      return m_siImage;
    }

    public virtual IntPtr UnpinObject()
    {
      m_siImage.id = m_Id.UnpinObject();
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
    ~ImageItem()
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

  /// <summary>
  /// class to handle image items referring to resource items in dll files
  /// </summary>
  public class ImageResource : ImageItem
  {
    private class resource
    {
      public UTF8String id = new UTF8String();
      public UTF8String type = new UTF8String();
    }

    private resource m_resource = new resource();
    private UTF8String m_filename = new UTF8String();

    public ImageResource(string resourceDllPath, string resourceId, string resourceType, int index = 0, string id = "")
      : base(id)
    {
      m_siImage.u.resource.index = index;

      m_filename.Value = resourceDllPath;
      m_resource.id.Value = resourceId;
      m_resource.type.Value = resourceType;
    }

    public override SiApp.SiImage_s PinObject()
    {
      m_siImage.u.resource.file_name = m_filename.PinObject();
      m_siImage.u.resource.id = m_resource.id.PinObject();
      m_siImage.u.resource.type = m_resource.type.PinObject();

      m_siImage.type = SiApp.SiImageType_t.e_resource_file;

      return base.PinObject();
    }

    public override IntPtr UnpinObject()
    {
      m_siImage.type = SiApp.SiImageType_t.e_none;

      m_filename.UnpinObject();
      m_resource.id.UnpinObject();
      m_resource.type.UnpinObject();

      return base.UnpinObject();
    }
  }

  /// <summary>
  /// class to handle image items referring to System.Drawing.Image objects
  /// </summary>
  public class ImageData : ImageItem
  {
    private class imageData
    {
      public PinnedObject<byte[]> data = new PinnedObject<byte[]>();
    }

    private imageData m_imageData = new imageData();

    public ImageData(Image image, int index = 0, string id = "")
      : base(id)
    {
      m_imageData.data.Value = ImageToByte(image);
      m_siImage.u.image.index = index;
      m_siImage.u.image.size = (uint)m_imageData.data.Value.Length;
    }

    public override SiApp.SiImage_s PinObject()
    {
      m_siImage.u.image.data = m_imageData.data.PinObject();
      m_siImage.type = SiApp.SiImageType_t.e_image;

      return base.PinObject();
    }

    public override IntPtr UnpinObject()
    {
      m_siImage.type = SiApp.SiImageType_t.e_none;
      m_imageData.data.UnpinObject();

      return base.UnpinObject();
    }

    private static byte[] ImageToByte(Image img)
    {
      ImageConverter converter = new ImageConverter();
      return (byte[])converter.ConvertTo(img, typeof(byte[]));
    }
  }

  /// <summary>
  /// class to handle image items referring to bitmap files
  /// </summary>
  public class ImageFile : ImageItem
  {
    private UTF8String m_filename = new UTF8String();

    public ImageFile(string filename, int index = 0, string id = "")
      : base(id)
    {
      m_filename.Value = filename;
      m_siImage.u.file.index = index;
    }

    public override SiApp.SiImage_s PinObject()
    {
      m_siImage.u.file.file_name = m_filename.PinObject();
      m_siImage.type = SiApp.SiImageType_t.e_image_file;

      return base.PinObject();
    }

    public override IntPtr UnpinObject()
    {
      m_siImage.type = SiApp.SiImageType_t.e_none;
      m_filename.UnpinObject();

      return base.UnpinObject();
    }
  }
}
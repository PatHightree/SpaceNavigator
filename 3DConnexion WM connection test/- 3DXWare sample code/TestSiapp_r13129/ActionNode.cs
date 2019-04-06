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
using System.Runtime.InteropServices;

namespace _3Dconnexion
{
  public struct ActionSet
  {
    public ActionSet(string id, string label)
    {
      Id = id;
      Label = label;
    }

    #region Properties

    public string Id;
    public string Label;

    #endregion Properties
  }

  public struct Category
  {
    public Category(string id, string label)
    {
      Id = id;
      Label = label;
    }

    #region Properties

    public string Id;
    public string Label;

    #endregion Properties
  }

  /// <summary>
  /// Structure representing an application Action / toolbar item
  /// </summary>
  public struct Action
  {
    public Action(string id, string label, string description = "", ImageItem image = null)
    {
      Id = id;
      Label = label;
      Description = description;
      Image = image;
    }

    #region Properties

    public string Id;
    public string Label;
    public string Description;
    public ImageItem Image;

    #endregion Properties
  }

  public class ActionNode : IDisposable
  {
    #region internal variables

    private SiApp.SiActionNodeEx_t _node;
    private ImageItem _image = null;

    private IntPtr pinned_node;

    private List<ActionNode> children = new List<ActionNode>();

    #endregion internal variables

    public static implicit operator SiApp.SiActionNodeEx_t(ActionNode action)
    {
      return action._node;
    }

    protected ActionNode()
    {
      _node.size = (uint)Marshal.SizeOf(typeof(SiApp.SiActionNodeEx_t));
    }

    private ActionNode(Action action)
      : this()
    {
      Id = action.Id;
      Label = action.Label;
      Description = action.Description;
      Type = SiApp.SiActionNodeType_t.SI_ACTION_NODE;
      Image = action.Image;
    }

    private ActionNode(Category category)
      : this()
    {
      Id = category.Id;
      Label = category.Label;
      Type = SiApp.SiActionNodeType_t.SI_CATEGORY_NODE;
    }

    private ActionNode(ActionSet actionset)
      : this()
    {
      Id = actionset.Id;
      Label = actionset.Label;
      Type = SiApp.SiActionNodeType_t.SI_ACTIONSET_NODE;
    }

    #region Properties

    private UTF8String m_id = new UTF8String();

    public string Id
    {
      get
      {
        return m_id.Value;
      }
      set
      {
        m_id.Value = value;
        if (IsPinned)
        {
          m_id.UnpinObject();
          Marshal.WriteIntPtr(pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), @"id"), m_id.PinObject());
        }
      }
    }

    private UTF8String m_label = new UTF8String();

    public string Label
    {
      get
      {
        return m_label.Value;
      }
      set
      {
        m_label.Value = value;
        if (IsPinned)
        {
          m_label.UnpinObject();
          Marshal.WriteIntPtr(pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), @"label"), m_label.PinObject());
        }
      }
    }

    private UTF8String m_description = new UTF8String();

    public string Description
    {
      get
      {
        return m_description.Value;
      }
      set
      {
        m_description.Value = value;
        if (IsPinned)
        {
          m_description.UnpinObject();
          Marshal.WriteIntPtr(pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), @"description"), m_description.PinObject());
        }
      }
    }

    public SiApp.SiActionNodeType_t Type
    {
      get
      {
        return _node.type;
      }
      set
      {
        if (value == SiApp.SiActionNodeType_t.SI_ACTION_NODE && children.Count != 0)
          throw new ArgumentException("Invalid type for parent node");
        _node.type = value;
        if (IsPinned)
          Marshal.WriteInt32(pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), @"type"), (int)_node.type);
      }
    }

    public ImageItem Image
    {
      get
      {
        return _image;
      }
      set
      {
        if (_image != null)
          _image.Dispose();
        _image = value;
        if (_image != null)
          _image.Id = Id;
      }
    }

    public int Count
    {
      get
      {
        return children.Count;
      }
    }

    public ActionNode this[int index]
    {
      get
      {
        return children[index];
      }
    }

    public bool IsPinned
    {
      get
      {
        return pinned_node != IntPtr.Zero;
      }
    }

    #endregion Properties

    #region Methods

    public ActionNode Add(ActionSet child)
    {
      return Add(new ActionNode(child));
    }

    public ActionNode Add(Category child)
    {
      return Add(new ActionNode(child));
    }

    public ActionNode Add(Action child)
    {
      return Add(new ActionNode(child));
    }

    public void Add(Action[] children)
    {
      for (int i = 0; i < children.Length; ++i)
        Add(new ActionNode(children[i]));
    }

    private ActionNode Add(ActionNode child)
    {
      if (Type == SiApp.SiActionNodeType_t.SI_ACTION_NODE)
        throw new ArgumentException("SI_ACTION_NODE type as parent node");

      if (pinned_node != IntPtr.Zero)
      {
        if (children.Count == 0)
        {
          _node.children = child.PinObject();
          Marshal.WriteIntPtr(pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), "children"), _node.children);
        }
        else
        {
          IntPtr next = child.PinObject();
          children[children.Count - 1]._node.next = next;
          Marshal.WriteIntPtr(children[children.Count - 1].pinned_node, (int)Marshal.OffsetOf(typeof(SiApp.SiActionNodeEx_t), "next"), next);
        }
      }

      children.Add(child);

      return child;
    }

    public void ForEach(Action<ActionNode> action)
    {
      for (int i = 0; i < children.Count; ++i)
      {
        children[i].ForEach(action);
      }
      action(this);
    }

    /// <summary>
    /// Marshal the SiApp.SiActionNodeEx_t structure
    /// </summary>
    /// <returns></returns>
    public IntPtr PinObject()
    {
      if (children.Count > 0)
      {
        for (int i = Count - 1; i > 0; --i)
        {
          children[i - 1]._node.next = children[i].PinObject();
        }
        _node.children = children[0].PinObject();
      }

      _node.id = m_id.PinObject();
      _node.label = m_label.PinObject();
      _node.description = m_description.PinObject();

      bool deleteOld = false;
      if (pinned_node == IntPtr.Zero)
        pinned_node = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SiApp.SiActionNodeEx_t)));
      else
        deleteOld = true;

      Marshal.StructureToPtr(_node, pinned_node, deleteOld);

      return pinned_node;
    }

    public IntPtr UnpinObject()
    {
      for (int i = 0; i < children.Count; ++i)
      {
        children[i].UnpinObject();
      }

      if (pinned_node != IntPtr.Zero)
      {
        Marshal.DestroyStructure(pinned_node, typeof(SiApp.SiActionNodeEx_t));
        pinned_node = IntPtr.Zero;
      }

      m_id.UnpinObject();
      m_label.UnpinObject();
      m_description.UnpinObject();
      return pinned_node;
    }

    #endregion Methods

    #region IDisposable Support

    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.
        for (int i = 0; i < children.Count; ++i)
        {
          children[i].Dispose(disposing);
        }

        if (pinned_node != IntPtr.Zero)
        {
          Marshal.DestroyStructure(pinned_node, typeof(SiApp.SiActionNodeEx_t));
          pinned_node = IntPtr.Zero;
        }

        if (disposing)
        {
          // dispose managed state (managed objects).
          children.Clear();

          m_id.Dispose();
          m_description.Dispose();
          m_label.Dispose();
        }

        disposedValue = true;
      }
    }

    // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~ActionNode()
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
}
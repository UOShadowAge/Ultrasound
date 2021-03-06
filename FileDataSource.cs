﻿// Decompiled with JetBrains decompiler
// Type: Voices.FileDataSource
// Assembly: Ultrasound, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 0A0C4A24-A00F-49E4-B8F5-373D63492914
// Assembly location: C:\Users\aliosatos\Desktop\UltraSound\Ultrasound.exe

using System.IO;

namespace Voices
{
  public class FileDataSource : DataSource
  {
    private string _data;

    public FileDataSource(string root)
    {
      this._data = root;
    }

    public override Stream Open(string file)
    {
      return (Stream) new FileStream(Path.Combine(this._data, file), FileMode.Open, FileAccess.Read);
    }

    public override bool Exists(string file)
    {
      return File.Exists(Path.Combine(this._data, file));
    }
  }
}

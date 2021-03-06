﻿using System.Collections.Generic;

using STR.Common.Contracts;


namespace UpkManager.Entities {

  public class UpkFile : ModelBase {

    public long FileSize { get; set; }

    public int GameVersion { get; set; }

    public string GameFilename { get; set; }

    public List<string> ExportTypes { get; set; }

    public string Notes { get; set; }

  }

}

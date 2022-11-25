using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BspFileScanner {
    public partial class MainForm: Form {
        private static string htmlTpl = @"<!DOCTYPE html>
<html lang=""zh-cmn-Hans"">
  <head>
    <meta charset=""UTF-8"" />
    <title>Portal 2 Workshop Files Viewer</title>
  </head>
  <style>
    .flex {
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
    }

    .gap-8 {
      gap: 8px;
    }

    .list-container.flex {
      flex-direction: row;
      flex-wrap: wrap;
      justify-content: flex-start;
      align-items: stretch;
      gap: 6px;
    }

    .item-wrap {
      border: 1px solid transparent;
      cursor: pointer;
    }

    .item-wrap:hover {
      border-color: red;
    }

    .def-img {
      height: 120px;
    }

    input {
      height: 24px;
      padding: 4px 8px;
      transition: all 0.3s, height 0s, width 0s;
      width: 600px;
      background: white;
      border: 1px solid #d9d9d9;
      border-radius: 2px;
      outline: none;
      font-size: 14px;
      font-family: v-mono, Consolas, SFMono-Regular, Menlo, Courier, v-sans, system-ui, -apple-system,
        BlinkMacSystemFont, 'Segoe UI', sans-serif, monospace, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';
    }

    input:hover {
      border-color: #669effff;
    }

    input:focus {
      border-color: #669effff;
      box-shadow: 0 0 0 2px #3d7fff33;
    }
  </style>
  <script src=""https://cdn.staticfile.org/vue/3.2.37/vue.global.min.js""></script>
  <body>
    <div id=""app"" class=""flex"">
      <h1 v-if=""!list?.length"">找不到任何内容</h1>
      <div v-else class=""flex gap-8"">
        <label>点击下方图片即可复制路径 <input type=""text"" ref=""hideInput"" /></label>
        <div class=""list-container flex"">
          <div class=""item-wrap flex"" v-for=""item in list"" @click=""copyPath(item)"">
            <div class=""img-wrap flex"">
              <img class=""def-img"" :src=""'file:///' + fullPath(item)"" alt="""" />
            </div>
            <div class=""flex"">{{item.dir}}</div>
          </div>
        </div>
      </div>
    </div>

    <script>
      const portal2workshop = 'portal2/maps/workshop'
      Vue.createApp({
        data() {
          return {
            path: '_FOLDER_',
            list: [_ITEMS_]
          }
        },
        methods: {
          concat(...strings) {
            let result = []
            if (strings?.length) {
              for (const str of strings) {
                let r = str.replaceAll('\\', '/')
                if (r.startsWith('/')) r = r.substring(1)
                if (r.endsWith('/')) r = r.substring(0, r.length - 1)
                result.push(r)
              }
            }
            return result.join('/')
          },
          fullPath(item) {
            return this.concat(this.path, portal2workshop, item.dir, item.img)
          },
          copyPath(item) {
            let str = this.concat(this.path, portal2workshop, item.dir)
            const input = this.$refs.hideInput
            input.value = str
            input.select()
            document.execCommand('copy')
          }
        }
      }).mount('#app')
    </script>
  </body>
</html>";
        public MainForm() {
            InitializeComponent();
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            toolStripStatusLabel1.Text = versionInfo.CompanyName + "制作";
            toolStripStatusLabel2.Text = "v" + versionInfo.ProductVersion;
            toolStripStatusLabel3.Text = versionInfo.LegalCopyright;
            folderBrowserDialog1.Description = "选择 Portal 2 的安装目录";
            textBox1.Text = checkPortal2Path();
        }

        private string checkPortal2Path() {
            string steamPath = getSteamPath();
            if (!"".Equals(steamPath)) {
                string portal2Path = Path.Combine(steamPath, "steamapps\\common\\Portal 2");
                DirectoryInfo portal2Dir = new DirectoryInfo(portal2Path);
                if (portal2Dir.Exists) {
                    return portal2Path;
                }
            }
            return steamPath;
        }

        private string getSteamPath() {
            string path = null;
            RegistryKey sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam");
            if (sub != null) {
                path = (string) sub.GetValue("SteamPath", null);
            }
            if (path == null) {
                sub = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
                if (sub != null) {
                    path = (string) sub.GetValue("InstallPath", null);
                }
            }
            return path ?? string.Empty;
        }

        private void button1_Click(object sender, EventArgs e) {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK) {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            try {
                DirectoryInfo directoryInfo = new DirectoryInfo(textBox1.Text);
                if (!directoryInfo.Exists) {
                    throw new Exception("文件夹不存在");
                }
                DirectoryInfo workshopDir = new DirectoryInfo(Path.Combine(textBox1.Text, "portal2\\maps\\workshop"));
                if (!workshopDir.Exists) {
                    throw new Exception("找不到工坊文件目录，请确认 Portal 2 的安装目录是否正确。");
                }
                DirectoryInfo[] subDirs = workshopDir.GetDirectories();
                List<BSPItem> list = new List<BSPItem>();
                foreach (DirectoryInfo subDir in subDirs) {
                    FileInfo[] files = subDir.GetFiles();
                    FileInfo img = findBspAndGetJpg(files);
                    if (img != null) {
                        list.Add(new BSPItem(img.Directory.Name, img.Name));
                    }
                }
                if (list.Count > 0) {
                    string listStr = string.Join(", ", list);
                    string outStr = htmlTpl.Replace("_FOLDER_", textBox1.Text.Replace("\\", "/")).Replace("_ITEMS_", listStr);
                    string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "bsp-scanner.html");
                    File.WriteAllText(htmlPath, outStr);
                    Process.Start("file:///" + htmlPath);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private FileInfo findBspAndGetJpg(FileInfo[] files) {
            bool findBsp = false;
            FileInfo img = null;
            foreach (FileInfo file in files) {
                String suffix = file.Name;
                suffix = suffix.Substring(suffix.LastIndexOf(".") + 1).ToLower();
                switch (suffix) {
                    case "bsp":
                        findBsp = true;
                        break;
                    case "jpg":
                    case "png":
                        img = file;
                        break;
                }
            }
            return findBsp ? img : null;
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e) {
            Process.Start("https://github.com/pansong291/BspFileScanner");
        }
    }

    public class BSPItem {
        public string dir { get; set; }
        public string img { get; set; }
        public BSPItem(string d, string i) {
            dir = d;
            img = i;
        }
        override
        public string ToString() {
            return "{dir:'" + dir.Replace("'", "\\'").Replace("\\", "/") + "',img:'" + img.Replace("'", "\\'").Replace("\\", "/") + "'}";
        }
    }
}

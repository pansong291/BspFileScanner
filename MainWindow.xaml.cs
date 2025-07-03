using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BspFileScanner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow: Window {
    private const string HtmlTpl =
        """
        <!DOCTYPE html>
        <html lang="zh-cmn-Hans">
        <head>
          <meta charset="UTF-8" />
          <title>Portal 2 Workshop Files Viewer</title>
        </head>
        <style>
          label {
            user-select: none;
          }
        
          label:has(input[type=checkbox]) {
            display: flex;
            align-items: center;
            gap: 4px;
            font-size: 14px;
          }
        
          #app {
            display: flex;
            justify-content: center;
            align-items: center;
          }
        
          .main {
            display: flex;
            flex-direction: column;
            gap: 8px;
          }
        
          #filter-wrap {
            display: flex;
            align-items: center;
            gap: 8px;
            user-select: none;
          }
        
          input {
            transition: border-color 0.3s, box-shadow 0.3s;
            background: white;
            border: 1px solid #d9d9d9;
            border-radius: 2px;
            outline: none;
            font-size: 14px;
            font-family: v-mono, Consolas, SFMono-Regular, Menlo, Courier, v-sans, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif, monospace, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';
          }
        
          input:hover {
            border-color: #3d7fff;
          }
        
          input:focus {
            border-color: #3d7fff;
            box-shadow: 0 0 0 2px #3d7fff33;
          }
        
          input[type=text] {
            padding: 4px 8px;
            height: 24px;
            width: 600px;
          }
        
          input[type=checkbox] {
            border: none;
            margin: 0;
            width: 14px;
            height: 14px;
          }
        
          #list-container {
            display: flex;
            flex-direction: row;
            flex-wrap: wrap;
            justify-content: flex-start;
            align-items: stretch;
            gap: 8px;
          }
        
          .item-wrap {
            cursor: pointer;
            border-radius: 4px;
            overflow: hidden;
            box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
            transition: box-shadow 0.3s;
          }
        
          .item-wrap:hover {
            box-shadow: 0 3px 6px rgba(0, 0, 0, 0.16), 0 3px 6px rgba(0, 0, 0, 0.23);
          }
        
          .img-wrap {
            overflow: hidden;
          }
        
          .def-img {
            display: block;
            height: 120px;
            min-width: 120px;
            transform: scale(1);
            transition: transform 0.3s;
            user-select: none;
          }
        
          .def-img:hover {
            transform: scale(1.05);
          }
        
          .title-wrap {
            display: flex;
            justify-content: start;
            align-items: center;
            gap: 2px;
            padding: 4px 8px;
          }
        
          .title-wrap:has(.tag-bsp) {
            padding-left: 4px;
          }
        
          .tag-bsp {
            border: 1px solid #3d7fff;
            border-radius: 4px;
            color: #3d7fff;
            padding: 0 4px;
            font-size: 12px;
            transform: scale(0.8);
          }
        
          #modal {
            display: none;
            justify-content: center;
            align-items: center;
            position: fixed;
            z-index: 1000;
            inset: 0;
            overflow: auto;
            background-color: rgba(0, 0, 0, 0.9);
            user-select: none;
          }
        
          #modal-img {
            display: block;
            max-width: 100%;
            max-height: 100%;
            object-fit: contain;
            transform: scale(0.1);
            transition: transform 0.3s;
          }
        
          #modal-close {
            position: absolute;
            top: 15px;
            right: 35px;
            color: #f1f1f1;
            font-size: 24px;
            font-weight: bold;
            transition: color 0.3s;
            cursor: pointer;
          }
        
          #modal-close:hover, #modal-close:focus {
            color: #bbb;
          }
        
          #toast-container {
            position: fixed;
            top: 16px;
            left: 32px;
            right: 32px;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            pointer-events: none;
          }
        
          .toast-wrap {
            --toast-height: auto;
            --toast-opacity: 0;
            --toast-transform: translateY(-16px);
            box-sizing: border-box;
            height: var(--toast-height);
            padding: 4px 8px;
            color: white;
            background: rgba(0, 0, 0, 0.85);
            border-radius: 6px;
            box-shadow: 0 6px 16px 0 rgba(0, 0, 0, 0.08), 0 3px 6px -4px rgba(0, 0, 0, 0.12), 0 9px 28px 8px rgba(0, 0, 0, 0.05);
            text-wrap: wrap;
            pointer-events: none;
            opacity: var(--toast-opacity);
            margin-top: 16px;
            transform: var(--toast-transform);
            transition: all 0.3s;
          }
        
          .toast-wrap.move-out {
            opacity: 0;
            transform: translateY(-16px);
            height: 0;
            padding-top: 0;
            padding-bottom: 0;
            margin-top: 0;
            margin-bottom: 0;
          }
        </style>
        <body>
        <div id="app"></div>
        <div id="modal">
          <img id="modal-img" src="#" alt="" />
          <div id="modal-close">&times;</div>
        </div>
        <div id="toast-container"></div>
        <template id="tpl-main">
          <div class="main">
            <label>点击下方名称即可复制路径 <input type="text" id="ipt-path" /></label>
            <div id="filter-wrap">筛选
              <label><input type="checkbox" id="cb-has-bsp" checked /> 有 Bsp</label>
              <label><input type="checkbox" id="cb-no-bsp" checked /> 无 Bsp</label>
            </div>
            <div id="list-container"></div>
          </div>
        </template>
        <template id="tpl-bsp-item">
          <div class="item-wrap">
            <div class="img-wrap">
              <img class="def-img" src="#" alt="" />
            </div>
            <div class="title-wrap">
              <div class="tag-bsp">BSP</div>
            </div>
          </div>
        </template>

        <script>
          const portal2Path = '_FOLDER_'
          const portal2workshop = 'portal2/maps/workshop'
          const list = [_ITEMS_]
          const elm = {}
        
          init()
        
          function init() {
            const app = document.getElementById('app')
            elm.toastContainer = document.getElementById('toast-container')
            if (list?.length) {
              const mainTpl = document.getElementById('tpl-main')
              const fragment = document.importNode(mainTpl.content, true)
              elm.iptPath = fragment.getElementById('ipt-path')
              elm.filterWrap = fragment.getElementById('filter-wrap')
              elm.cbHasBsp = fragment.getElementById('cb-has-bsp')
              elm.cbNoBsp = fragment.getElementById('cb-no-bsp')
              elm.listContainer = fragment.getElementById('list-container')
              elm.modal = document.getElementById('modal')
              elm.modalImg = document.getElementById('modal-img')
              app.append(fragment)
              for (let it of list) {
                initItem(it)
              }
              elm.filterWrap.addEventListener('click', (e) => {
                const label = e.target.closest('label')
                if (!label) return
                for (let child of elm.listContainer.children) {
                  const cb = child.querySelector('.tag-bsp') ? elm.cbHasBsp : elm.cbNoBsp
                  child.style.display = cb.checked ? 'block' : 'none'
                }
              })
              elm.listContainer.addEventListener('click', (e) => {
                if (e.target.classList.contains('def-img')) {
                  elm.modal.style.display = 'flex'
                  elm.modalImg.src = e.target.src
                  document.body.style.overflow = 'hidden'
                  setTimeout(() => {
                    elm.modalImg.style.transform = 'scale(1)'
                  }, 10)
                } else {
                  const titleWrap = e.target.closest('.title-wrap')
                  if (titleWrap) {
                    copyDirPath(titleWrap.dataset.dir)
                    toast('已复制')
                  }
                }
              })
              const closeModal = () => {
                elm.modal.style.display = 'none'
                elm.modalImg.style.transform = 'scale(0.1)'
                document.body.style.overflow = ''
              }
              elm.modal.addEventListener('click', (e) => {
                if (e.target !== elm.modalImg) closeModal()
              })
              document.addEventListener('keydown', (e) => {
                if (elm.modal.style.display === 'flex' && e.key === 'Escape') closeModal()
              })
              elm.modalImg.addEventListener('load', () => {
                elm.modalImg.style.aspectRatio = elm.modalImg.naturalWidth + '/' + elm.modalImg.naturalHeight
              })
            } else {
              const h1 = document.createElement('h1')
              h1.innerText = '找不到任何内容'
              app.append(h1)
            }
          }
        
          function initItem(item) {
            const itemTpl = document.getElementById('tpl-bsp-item')
            const fragment = document.importNode(itemTpl.content, true)
            fragment.querySelector('img').src = 'file:///' + imgPathOf(item)
            const titleWrap = fragment.querySelector('.title-wrap')
            titleWrap.append([item.dir])
            titleWrap.dataset.dir = item.dir
            titleWrap.dataset.bsp = item.bsp
            if (!item.bsp) titleWrap.querySelector('.tag-bsp').remove()
            elm.listContainer.append(fragment)
          }
        
          function copyDirPath(dir) {
            elm.iptPath.value = concat(portal2Path, portal2workshop, dir)
            elm.iptPath.select()
            document.execCommand('copy')
          }
        
          function imgPathOf(item) {
            return concat(portal2Path, portal2workshop, item.dir, item.img)
          }
        
          function concat(...strings) {
            const result = []
            if (strings?.length) {
              for (const str of strings) {
                let r = str.replaceAll('\\', '/')
                if (r.startsWith('/')) r = r.substring(1)
                if (r.endsWith('/')) r = r.substring(0, r.length - 1)
                result.push(r)
              }
            }
            return result.join('/')
          }
        
          function toast(msg, dur = 2000) {
            const toastItem = document.createElement('div')
            toastItem.classList.add('toast-wrap')
            toastItem.innerText = msg
            elm.toastContainer.append(toastItem)
        
            let timer = 0
            toastItem.addEventListener('transitionend', () => {
              clearTimeout(timer)
              timer = setTimeout(() => {
                if (toastItem.classList.contains('move-out')) toastItem.remove()
              }, 10)
            })
        
            setTimeout(() => {
              toastItem.style.setProperty('--toast-height', toastItem.offsetHeight + 'px')
              toastItem.style.setProperty('--toast-opacity', '1')
              toastItem.style.setProperty('--toast-transform', 'translateY(0)')
            }, 10)
        
            setTimeout(() => {
              toastItem.classList.add('move-out')
            }, dur)
          }
        </script>
        </body>
        </html>
        """;

    public MainWindow() {
        InitializeComponent();

        // 获取版本信息
        var entryAssemblyPath = Environment.ProcessPath!;
        var versionInfo = FileVersionInfo.GetVersionInfo(entryAssemblyPath);

        // 提取主要版本号（忽略构建元数据）
        var productVersion = versionInfo.ProductVersion;
        if (!string.IsNullOrEmpty(productVersion)) {
            // 分割版本号和构建元数据（如果存在）
            var versionParts = productVersion.Split('+');
            productVersion = versionParts[0];
        }
        VersionTextBlock.Text = "v" + productVersion;
        CopyrightTextBlock.Text = versionInfo.LegalCopyright;
        FolderPathBox.Text = CheckPortal2Path();
    }

    private static string CheckPortal2Path() {
        var steamPath = GetSteamPath();
        if ("".Equals(steamPath)) return steamPath;
        var portal2Path = Path.Combine(steamPath, @"steamapps\common\Portal 2");
        var portal2Dir = new DirectoryInfo(portal2Path);
        return portal2Dir.Exists ? portal2Path : steamPath;
    }

    private static string GetSteamPath() {
        var path = (string?) Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam")?.GetValue("SteamPath", null);
        if (string.IsNullOrEmpty(path)) {
            path = (string?) Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
                ?.GetValue("InstallPath", null);
        }
        if (path == null) return string.Empty;

        var idx = path.IndexOf(':');
        if (idx > 0) path = path[..idx].ToUpper() + path[idx..];
        return path.Replace('/', '\\');
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
        try {
            Process.Start(new ProcessStartInfo {
                UseShellExecute = true,
                FileName = e.Uri.AbsoluteUri,
                WorkingDirectory = string.Empty
            });
            e.Handled = true;
        } catch (Exception ex) {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChooseFolderButton_OnClick(object sender, RoutedEventArgs e) {
        var dialog = new CommonOpenFileDialog();
        dialog.IsFolderPicker = true;
        dialog.Title = "选择 Portal 2 的安装目录";
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
        var selectedPath = dialog.FileName;
        FolderPathBox.Text = selectedPath.Replace('/', '\\');
    }

    private void ScanButton_OnClick(object sender, RoutedEventArgs e) {
        try {
            var directoryInfo = new DirectoryInfo(FolderPathBox.Text);
            if (!directoryInfo.Exists) {
                throw new Exception("文件夹不存在");
            }
            var workshopDir = new DirectoryInfo(Path.Combine(FolderPathBox.Text, @"portal2\maps\workshop"));
            if (!workshopDir.Exists) {
                throw new Exception("找不到工坊文件目录，请确认 Portal 2 的安装目录是否正确。");
            }
            var subDirs = workshopDir.GetDirectories();
            var list = subDirs.Select(FindBspAndGetJpg).OfType<BspItem>().ToList();
            if (list.Count <= 0) throw new Exception("没有可显示的工坊文件");
            var listStr = string.Join(", ", list);
            var outStr = HtmlTpl.Replace("_FOLDER_", FolderPathBox.Text.Replace('\\', '/'))
                .Replace("_ITEMS_", listStr);
            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "bsp-scanner.html");
            File.WriteAllText(htmlPath, outStr);
            Process.Start(new ProcessStartInfo {
                UseShellExecute = true,
                FileName = "file:///" + htmlPath,
                WorkingDirectory = string.Empty
            });
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static BspItem? FindBspAndGetJpg(DirectoryInfo dir) {
        var findBsp = false;
        FileInfo? img = null;
        var files = dir.GetFiles();
        foreach (var file in files) {
            var suffix = file.Name;
            suffix = suffix[(suffix.LastIndexOf('.') + 1)..].ToLower();
            switch (suffix) {
                case "bsp":
                    findBsp = true;
                    break;
                case "jpg":
                case "jpeg":
                case "png":
                case "gif":
                    img = file;
                    break;
            }
        }
        return img == null ? null : new BspItem(dir.Name, img.Name, findBsp);
    }
}

public class BspItem(string d, string i, bool b) {
    private string Dir { get; } = d;
    private string Img { get; } = i;
    private bool Bsp { get; } = b;

    override
        public string ToString() {
        var dir = Dir.Replace("'", "\\'").Replace('\\', '/');
        var img = Img.Replace("'", "\\'").Replace('\\', '/');
        var bsp = Bsp ? "true" : "false";
        return "{dir:'" + dir + "',img:'" + img + "',bsp:" + bsp + "}";
    }
}

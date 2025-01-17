﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Peek.Common.Models;
using Peek.UI.Extensions;
using SHDocVw;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using IServiceProvider = Peek.Common.Models.IServiceProvider;

namespace Peek.UI.Helpers
{
    public static class FileExplorerHelper
    {
        internal static IShellItemArray? GetSelectedItems(HWND foregroundWindowHandle)
        {
            return GetItemsInternal(foregroundWindowHandle, onlySelectedFiles: true);
        }

        internal static IShellItemArray? GetItems(HWND foregroundWindowHandle)
        {
            return GetItemsInternal(foregroundWindowHandle, onlySelectedFiles: false);
        }

        private static IShellItemArray? GetItemsInternal(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            if (foregroundWindowHandle.IsDesktopWindow())
            {
                return GetItemsFromDesktop(foregroundWindowHandle, onlySelectedFiles);
            }
            else
            {
                return GetItemsFromFileExplorer(foregroundWindowHandle, onlySelectedFiles);
            }
        }

        private static IShellItemArray? GetItemsFromDesktop(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            const int SWC_DESKTOP = 8;
            const int SWFO_NEEDDISPATCH = 1;

            var shell = new Shell32.Shell();
            ShellWindows shellWindows = shell.Windows();

            object? oNull1 = null;
            object? oNull2 = null;

            var serviceProvider = (IServiceProvider)shellWindows.FindWindowSW(ref oNull1, ref oNull2, SWC_DESKTOP, out int pHWND, SWFO_NEEDDISPATCH);
            var shellBrowser = (IShellBrowser)serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, typeof(IShellBrowser).GUID);

            IShellItemArray? shellItemArray = GetShellItemArray(shellBrowser, onlySelectedFiles);
            return shellItemArray;
        }

        private static IShellItemArray? GetItemsFromFileExplorer(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            IShellItemArray? shellItemArray = null;

            var activeTab = foregroundWindowHandle.GetActiveTab();

            var shell = new Shell32.Shell();
            ShellWindows shellWindows = shell.Windows();
            foreach (IWebBrowserApp webBrowserApp in shell.Windows())
            {
                var shellFolderView = (Shell32.IShellFolderViewDual2)webBrowserApp.Document;
                var folderTitle = shellFolderView.Folder.Title;

                if (webBrowserApp.HWND == foregroundWindowHandle)
                {
                    var serviceProvider = (IServiceProvider)webBrowserApp;
                    var shellBrowser = (IShellBrowser)serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, typeof(IShellBrowser).GUID);
                    shellBrowser.GetWindow(out IntPtr shellBrowserHandle);

                    if (activeTab == shellBrowserHandle)
                    {
                        shellItemArray = GetShellItemArray(shellBrowser, onlySelectedFiles);
                        return shellItemArray;
                    }
                }
            }

            return shellItemArray;
        }

        private static IShellItemArray? GetShellItemArray(IShellBrowser shellBrowser, bool onlySelectedFiles)
        {
            var shellView = (IFolderView)shellBrowser.QueryActiveShellView();
            var selectionFlag = onlySelectedFiles ? (uint)_SVGIO.SVGIO_SELECTION : (uint)_SVGIO.SVGIO_ALLVIEW;
            shellView.Items(selectionFlag, typeof(IShellItemArray).GUID, out var items);
            return items as IShellItemArray;
        }
    }
}

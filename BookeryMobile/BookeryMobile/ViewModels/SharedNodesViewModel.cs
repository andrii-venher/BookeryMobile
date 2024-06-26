﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BookeryMobile.Common;
using BookeryMobile.Data.DTOs.Node.Input;
using BookeryMobile.Data.DTOs.Node.Output;
using BookeryMobile.Data.Models;
using BookeryMobile.Exceptions;
using BookeryMobile.Services.Node.Interfaces;
using BookeryMobile.Services.Storage;
using BookeryMobile.Views;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BookeryMobile.ViewModels
{
    class SharedNodesViewModel : BaseViewModel
    {
        private readonly string _path;
        private readonly ISharedNodeService _sharedNodeService = DependencyService.Get<ISharedNodeService>();
        private readonly ISharingService _sharingService = DependencyService.Get<ISharingService>();
        private readonly IStorageService _storageService = DependencyService.Get<IStorageService>();
        private readonly IMessage _message = DependencyService.Get<IMessage>();
        private readonly INavigation _navigation;
        private PopupPage? _page;

        public SharedNodesViewModel(INavigation navigation, string path)
        {
            _navigation = navigation;
            _path = path.Trim('/');
            if (_path.Length > 0)
            {
                if (_path.Contains("/"))
                {
                    Title = _path.Substring(_path.LastIndexOf('/') + 1);
                }
                else
                {
                    SetRootTitle(Guid.Parse(_path));
                }
            }
            else
            {
                Title = "Shared";
            }

            Nodes = new ObservableCollection<NodeElement>();

            LoadNodesCommand = new Command(LoadNodes);
            SelectNodeCommand = new Command<NodeDto?>(SelectNode);
            RenameNodeCommand = new Command<NodeDto?>(OpenRenameNodePopup);
            CreateDirectoryCommand = new Command(OpenCreateDirectoryPopup, () => _path.Length > 0);
            UploadFileCommand = new Command(UploadFile, () => _path.Length > 0);

            PopupNavigation.Instance.Popping += (sender, args) =>
            {
                if (PopupNavigation.Instance.PopupStack.Count > 0 && args.Page == _page)
                {
                    OnAppearing();
                }
            };
        }

        public ObservableCollection<NodeElement> Nodes { get; }

        public Command LoadNodesCommand { get; }
        public Command<NodeDto?> SelectNodeCommand { get; }
        public Command<NodeDto?> RenameNodeCommand { get; }
        public Command CreateDirectoryCommand { get; }
        public Command UploadFileCommand { get; }

        private async void LoadNodes()
        {
            IsBusy = true;
            Nodes.Clear();
            var nodes = await _sharedNodeService.Get(_path);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    Nodes.Add(new NodeElement
                    {
                        Node = node,
                        ImageSource = FileIconHelper.GetImageSource(node)
                    });
                }
            }

            IsBusy = false;
        }

        private async void SelectNode(NodeDto? node)
        {
            if (node != null)
            {
                if (node.IsDirectory)
                {
                    if (_path.Length == 0)
                    {
                        await _navigation.PushAsync(new SharedNodesPage(node.Id.ToString()));
                    }
                    else
                    {
                        await _navigation.PushAsync(new SharedNodesPage(_path + "/" + node.Name));
                    }
                }
                else
                {
                    await PopupNavigation.Instance.PushAsync(new FileActionsPage(new FileActionsViewModel(node)));
                }
            }
        }

        private void OpenRenameNodePopup(NodeDto? node)
        {
            if (node != null)
            {
                PushPopupPage(new AlterNodePage(new RenameNodeViewModel(PopupNavigation.Instance, _sharedNodeService,
                    Path.Combine(_path, (_path.Length > 0) ? node.Name : node.Id.ToString()), node)));
            }
        }

        private void OpenCreateDirectoryPopup()
        {
            PushPopupPage(
                new AlterNodePage(new CreateDirectoryViewModel(PopupNavigation.Instance, _sharedNodeService, _path)));
        }

        private async void UploadFile()
        {
            try
            {
                var file = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select file"
                });
                if (file != null)
                {
                    PushPopupPage(new LoadingPage());

                    var fileName = file.FileName;
                    NodeDto? node = null;
                    
                    try
                    {
                        node = await _sharedNodeService.Create(_path, new CreateNodeDto(fileName, false));
                    }
                    catch (NameConflictException)
                    {
                        var existingNodeElement = Nodes.FirstOrDefault(x => x.Node.Name == fileName);
                        if (existingNodeElement != null)
                        {
                            node = existingNodeElement.Node;
                        }
                    }
                    
                    if (node == null)
                    {
                        _message.Short("Unexpected error occurred.");
                        return;
                    }
                    
                    var stream = await file.OpenReadAsync();
                    var result = await _storageService.Upload(node.Id, stream, fileName);

                    if (!result)
                    {
                        _message.Short("Unexpected error occurred.");
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                PopPopupPage();
            }
        }

        public void OnAppearing()
        {
            IsBusy = true;
        }

        private async void PushPopupPage(PopupPage page)
        {
            _page = page;
            await PopupNavigation.Instance.PushAsync(_page);
        }

        private async void PopPopupPage()
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }

        private async void SetRootTitle(Guid id)
        {
            var rootNode = await _sharingService.Details(id);
            if (rootNode != null)
            {
                Title = rootNode.Name;
            }
        }
    }
}
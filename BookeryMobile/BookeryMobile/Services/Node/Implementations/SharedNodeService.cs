﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BookeryMobile.Data.DTOs.Node.Input;
using BookeryMobile.Data.DTOs.Node.Output;
using BookeryMobile.Exceptions;
using BookeryMobile.Services.Common;
using BookeryMobile.Services.Node.Interfaces;

namespace BookeryMobile.Services.Node.Implementations
{
    public class SharedNodeService : BaseService, ISharedNodeService
    {
        public SharedNodeService() : base("Node/Shared")
        {
        }

        public async Task<List<NodeDto>?> Get(string path)
        {
            var response = await HttpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var nodes = await response.Content.ReadAsAsync<List<NodeDto>>();
                return nodes;
            }

            return null;
        }

        public async Task<NodeDto?> Create(string path, CreateNodeDto create)
        {
            var response = await HttpClient.PostAsJsonAsync(path, create);

            if (response.IsSuccessStatusCode)
            {
                var node = await response.Content.ReadAsAsync<NodeDto>();
                return node;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new NameConflictException();
            }

            return null;
        }

        public async Task<string?> Update(string path, UpdateNodeDto update)
        {
            var response = await HttpClient.PutAsJsonAsync(path, update);

            if (response.IsSuccessStatusCode)
            {
                var newPath = await response.Content.ReadAsStringAsync();
                return newPath;
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Forbidden => throw new WrongAccessTypeException(),
                HttpStatusCode.Conflict => throw new NameConflictException(),
                _ => null
            };
        }
    }
}
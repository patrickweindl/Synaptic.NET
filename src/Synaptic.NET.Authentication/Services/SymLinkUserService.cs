using System.Security.Claims;
using System.Text.Json;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Authentication.Services;

public class SymLinkUserService : ISymLinkUserService
{
    private readonly SynapticServerSettings _settings;
    private List<SymLinkUserInfo> _symLinkUsers = new();
    public SymLinkUserService(SynapticServerSettings settings)
    {
        _settings = settings;
        Init();
    }

    private void Init()
    {
        _symLinkUsers = new();
        if (File.Exists(Path.Join(_settings.BaseDataPath, "symLinkUsers.json")))
        {
            var content = File.ReadAllText(Path.Join(_settings.BaseDataPath, "symLinkUsers.json"));
            var existingSymLinks = JsonSerializer.Deserialize<List<SymLinkUserInfo>>(content) ?? new List<SymLinkUserInfo>();
            _symLinkUsers.AddRange(existingSymLinks);
        }
        else
        {
            File.Create(Path.Join(_settings.BaseDataPath, "symLinkUsers.json")).Close();
            File.WriteAllText(JsonSerializer.Serialize(_symLinkUsers, JsonSerializerOptions.Default), Path.Join(_settings.BaseDataPath, "symLinkUsers.json"));
        }
    }

    private void AddSymLink(string mainUserIdentifier, string userIdentifier)
    {
        if (_symLinkUsers.FirstOrDefault(u => u.MainUserIdentifier == mainUserIdentifier) is { } symLink)
        {
            symLink.SymLinkUserIdentifiers.Add(userIdentifier);
        }
        else
        {
            _symLinkUsers.Add(new SymLinkUserInfo
            {
                MainUserIdentifier = mainUserIdentifier,
                SymLinkUserIdentifiers = [userIdentifier]
            });
        }
        File.WriteAllText(JsonSerializer.Serialize(_symLinkUsers, JsonSerializerOptions.Default), Path.Join(_settings.BaseDataPath, "symLinkUsers.json"));
    }

    private void RemoveSymLink(string mainUserIdentifier, string userIdentifier)
    {
        if (_symLinkUsers.FirstOrDefault(u => u.MainUserIdentifier == mainUserIdentifier) is { } symLink)
        {
            symLink.SymLinkUserIdentifiers.Remove(userIdentifier);
            File.WriteAllText(JsonSerializer.Serialize(_symLinkUsers, JsonSerializerOptions.Default), Path.Join(_settings.BaseDataPath, "symLinkUsers.json"));
        }
    }

    public List<ClaimsIdentity> GetSymLinkUsers(ClaimsIdentity claimsIdentity)
    {
        string storageId = $"{claimsIdentity.ToUserIdentifier()}";
        if (_symLinkUsers.FirstOrDefault(u =>
                u.MainUserIdentifier == storageId || u.SymLinkUserIdentifiers.Any(s => s == storageId)) is { } symLink)
        {
            List<ClaimsIdentity> identities = new();
            foreach (var symLinkUser in symLink.SymLinkUserIdentifiers)
            {
                string userName = symLinkUser.Split("__").First();
                string userId = symLinkUser.Split("__").Last();
                identities.Add(ClaimsHelper.FromUserNameAndId(userName, userId));
            }
            return identities.Concat([claimsIdentity]).ToList();
        }

        return [claimsIdentity];
    }

    public ClaimsIdentity GetMainIdentity(ClaimsIdentity claimsIdentity)
    {
        string storageId = $"{claimsIdentity.ToUserIdentifier()}";
        if (_symLinkUsers.FirstOrDefault(u =>
                u.MainUserIdentifier == storageId || u.SymLinkUserIdentifiers.Any(s => s == storageId)) is { } symLink)
        {
            return ClaimsHelper.FromUserNameAndId(symLink.MainUserIdentifier.Split("__").First(), symLink.MainUserIdentifier.Split("__").Last());
        }
        return claimsIdentity;
    }

    public void AddSymLink(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity)
    {
        string mainUserIdentifier = mainIdentity.ToUserIdentifier();
        string subUserIdentifier = subIdentity.ToUserIdentifier();
        AddSymLink(mainUserIdentifier, subUserIdentifier);
    }

    public void RemoveSymLink(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity)
    {
        string mainUserIdentifier = mainIdentity.ToUserIdentifier();
        string subUserIdentifier = subIdentity.ToUserIdentifier();
        RemoveSymLink(mainUserIdentifier, subUserIdentifier);
    }
}

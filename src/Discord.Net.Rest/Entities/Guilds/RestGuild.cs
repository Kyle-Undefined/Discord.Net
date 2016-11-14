﻿using Discord.API.Rest;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Guild;
using EmbedModel = Discord.API.GuildEmbed;
using System.Linq;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestGuild : RestEntity<ulong>, IGuild, IUpdateable
    {
        private ImmutableDictionary<ulong, RestRole> _roles;
        private ImmutableArray<GuildEmoji> _emojis;
        private ImmutableArray<string> _features;

        public string Name { get; private set; }
        public int AFKTimeout { get; private set; }
        public bool IsEmbeddable { get; private set; }
        public VerificationLevel VerificationLevel { get; private set; }
        public MfaLevel MfaLevel { get; private set; }
        public DefaultMessageNotifications DefaultMessageNotifications { get; private set; }
        
        public ulong? AFKChannelId { get; private set; }
        public ulong? EmbedChannelId { get; private set; }
        public ulong OwnerId { get; private set; }
        public string VoiceRegionId { get; private set; }
        public string IconId { get; private set; }
        public string SplashId { get; private set; }
        internal bool Available { get; private set; }

        public DateTimeOffset CreatedAt => DateTimeUtils.FromSnowflake(Id);
        public ulong DefaultChannelId => Id;
        public string IconUrl => API.CDN.GetGuildIconUrl(Id, IconId);
        public string SplashUrl => API.CDN.GetGuildSplashUrl(Id, SplashId);

        public RestRole EveryoneRole => GetRole(Id);
        public IReadOnlyCollection<RestRole> Roles => _roles.ToReadOnlyCollection();
        public IReadOnlyCollection<GuildEmoji> Emojis => _emojis;
        public IReadOnlyCollection<string> Features => _features;

        internal RestGuild(BaseDiscordClient client, ulong id)
            : base(client, id)
        {
        }
        internal static RestGuild Create(BaseDiscordClient discord, Model model)
        {
            var entity = new RestGuild(discord, model.Id);
            entity.Update(model);
            return entity;
        }
        internal void Update(Model model)
        {
            AFKChannelId = model.AFKChannelId;
            EmbedChannelId = model.EmbedChannelId;
            AFKTimeout = model.AFKTimeout;
            IsEmbeddable = model.EmbedEnabled;
            IconId = model.Icon;
            Name = model.Name;
            OwnerId = model.OwnerId;
            VoiceRegionId = model.Region;
            SplashId = model.Splash;
            VerificationLevel = model.VerificationLevel;
            MfaLevel = model.MfaLevel;
            DefaultMessageNotifications = model.DefaultMessageNotifications;

            if (model.Emojis != null)
            {
                var emojis = ImmutableArray.CreateBuilder<GuildEmoji>(model.Emojis.Length);
                for (int i = 0; i < model.Emojis.Length; i++)
                    emojis.Add(GuildEmoji.Create(model.Emojis[i]));
                _emojis = emojis.ToImmutableArray();
            }
            else
                _emojis = ImmutableArray.Create<GuildEmoji>();

            if (model.Features != null)
                _features = model.Features.ToImmutableArray();
            else
                _features = ImmutableArray.Create<string>();

            var roles = ImmutableDictionary.CreateBuilder<ulong, RestRole>();
            if (model.Roles != null)
            {
                for (int i = 0; i < model.Roles.Length; i++)
                    roles[model.Roles[i].Id] = RestRole.Create(Discord, this, model.Roles[i]);
            }
            _roles = roles.ToImmutable();

            Available = true;
        }
        internal void Update(EmbedModel model)
        {
            EmbedChannelId = model.ChannelId;
            IsEmbeddable = model.Enabled;
        }

        //General
        public async Task UpdateAsync(RequestOptions options = null)
            => Update(await Discord.ApiClient.GetGuildAsync(Id, options).ConfigureAwait(false));
        public Task DeleteAsync(RequestOptions options = null)
            => GuildHelper.DeleteAsync(this, Discord, options);

        public async Task ModifyAsync(Action<ModifyGuildParams> func, RequestOptions options = null)
        {
            var model = await GuildHelper.ModifyAsync(this, Discord, func, options).ConfigureAwait(false);
            Update(model);
        }
        public async Task ModifyEmbedAsync(Action<ModifyGuildEmbedParams> func, RequestOptions options = null)
        { 
            var model = await GuildHelper.ModifyEmbedAsync(this, Discord, func, options).ConfigureAwait(false);
            Update(model);
        }
        public async Task ModifyChannelsAsync(IEnumerable<ModifyGuildChannelsParams> args, RequestOptions options = null)
        {
            var arr = args.ToArray();
            await GuildHelper.ModifyChannelsAsync(this, Discord, arr, options);
        }
        public async Task ModifyRolesAsync(IEnumerable<ModifyGuildRolesParams> args, RequestOptions options = null)
        {
            var models = await GuildHelper.ModifyRolesAsync(this, Discord, args, options).ConfigureAwait(false);
            foreach (var model in models)
            {
                var role = GetRole(model.Id);
                if (role != null)
                    role.Update(model);
            }
        }

        public Task LeaveAsync(RequestOptions options = null)
            => GuildHelper.LeaveAsync(this, Discord, options);

        //Bans
        public Task<IReadOnlyCollection<RestBan>> GetBansAsync(RequestOptions options = null)
            => GuildHelper.GetBansAsync(this, Discord, options);

        public Task AddBanAsync(IUser user, int pruneDays = 0, RequestOptions options = null)
            => GuildHelper.AddBanAsync(this, Discord, user.Id, pruneDays, options);
        public Task AddBanAsync(ulong userId, int pruneDays = 0, RequestOptions options = null)
            => GuildHelper.AddBanAsync(this, Discord, userId, pruneDays, options);

        public Task RemoveBanAsync(IUser user, RequestOptions options = null)
            => GuildHelper.RemoveBanAsync(this, Discord, user.Id, options);
        public Task RemoveBanAsync(ulong userId, RequestOptions options = null)
            => GuildHelper.RemoveBanAsync(this, Discord, userId, options);

        //Channels
        public Task<IReadOnlyCollection<RestGuildChannel>> GetChannelsAsync(RequestOptions options = null)
            => GuildHelper.GetChannelsAsync(this, Discord, options);
        public Task<RestGuildChannel> GetChannelAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetChannelAsync(this, Discord, id, options);
        public Task<RestTextChannel> CreateTextChannelAsync(string name, RequestOptions options = null)
            => GuildHelper.CreateTextChannelAsync(this, Discord, name, options);
        public Task<RestVoiceChannel> CreateVoiceChannelAsync(string name, RequestOptions options = null)
            => GuildHelper.CreateVoiceChannelAsync(this, Discord, name, options);

        //Integrations
        public Task<IReadOnlyCollection<RestGuildIntegration>> GetIntegrationsAsync(RequestOptions options = null)
            => GuildHelper.GetIntegrationsAsync(this, Discord, options);
        public Task<RestGuildIntegration> CreateIntegrationAsync(ulong id, string type, RequestOptions options = null)
            => GuildHelper.CreateIntegrationAsync(this, Discord, id, type, options);

        //Invites
        public Task<IReadOnlyCollection<RestInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
            => GuildHelper.GetInvitesAsync(this, Discord, options);

        //Roles
        public RestRole GetRole(ulong id)
        {
            RestRole value;
            if (_roles.TryGetValue(id, out value))
                return value;
            return null;
        }

        public async Task<RestRole> CreateRoleAsync(string name, GuildPermissions? permissions = default(GuildPermissions?), Color? color = default(Color?), 
            bool isHoisted = false, RequestOptions options = null)
        {
            var role = await GuildHelper.CreateRoleAsync(this, Discord, name, permissions, color, isHoisted, options).ConfigureAwait(false);
            _roles = _roles.Add(role.Id, role);
            return role;
        }

        //Users
        public IAsyncEnumerable<IReadOnlyCollection<RestGuildUser>> GetUsersAsync(RequestOptions options = null)
            => GuildHelper.GetUsersAsync(this, Discord, null, null, options);
        public Task<RestGuildUser> GetUserAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetUserAsync(this, Discord, id, options);
        public Task<RestGuildUser> GetCurrentUserAsync(RequestOptions options = null)
            => GuildHelper.GetUserAsync(this, Discord, Discord.CurrentUser.Id, options);

        public Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null)
            => GuildHelper.PruneUsersAsync(this, Discord, days, simulate, options);

        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id})";

        //IGuild
        bool IGuild.Available => Available;
        IAudioClient IGuild.AudioClient => null;
        IRole IGuild.EveryoneRole => EveryoneRole;
        IReadOnlyCollection<IRole> IGuild.Roles => Roles;

        async Task<IReadOnlyCollection<IBan>> IGuild.GetBansAsync(RequestOptions options)
            => await GetBansAsync(options).ConfigureAwait(false);

        async Task<IReadOnlyCollection<IGuildChannel>> IGuild.GetChannelsAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetChannelsAsync(options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<IGuildChannel>();
        }
        async Task<IGuildChannel> IGuild.GetChannelAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetChannelAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        async Task<ITextChannel> IGuild.CreateTextChannelAsync(string name, RequestOptions options)
            => await CreateTextChannelAsync(name, options).ConfigureAwait(false);
        async Task<IVoiceChannel> IGuild.CreateVoiceChannelAsync(string name, RequestOptions options)
            => await CreateVoiceChannelAsync(name, options).ConfigureAwait(false);

        async Task<IReadOnlyCollection<IGuildIntegration>> IGuild.GetIntegrationsAsync(RequestOptions options)
            => await GetIntegrationsAsync(options).ConfigureAwait(false);
        async Task<IGuildIntegration> IGuild.CreateIntegrationAsync(ulong id, string type, RequestOptions options)
            => await CreateIntegrationAsync(id, type, options).ConfigureAwait(false);

        async Task<IReadOnlyCollection<IInviteMetadata>> IGuild.GetInvitesAsync(RequestOptions options)
            => await GetInvitesAsync(options).ConfigureAwait(false);

        IRole IGuild.GetRole(ulong id) 
            => GetRole(id);
        async Task<IRole> IGuild.CreateRoleAsync(string name, GuildPermissions? permissions, Color? color, bool isHoisted, RequestOptions options)
            => await CreateRoleAsync(name, permissions, color, isHoisted, options).ConfigureAwait(false);

        async Task<IGuildUser> IGuild.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetUserAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        async Task<IGuildUser> IGuild.GetCurrentUserAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetCurrentUserAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.GetUsersAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return (await GetUsersAsync(options).Flatten().ConfigureAwait(false)).ToImmutableArray();
            else
                return ImmutableArray.Create<IGuildUser>();
        }
        Task IGuild.DownloadUsersAsync() { throw new NotSupportedException(); }
    }
}
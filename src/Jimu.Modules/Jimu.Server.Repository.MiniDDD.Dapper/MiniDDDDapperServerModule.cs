﻿using Autofac;
using Jimu.Module;
using Microsoft.Extensions.Configuration;
using MiniDDD;
using MiniDDD.UnitOfWork;
using MiniDDD.UnitOfWork.Dapper;
using System;
using System.Linq;

namespace Jimu.Server.Repository.MiniDDD.Dapper
{
    public class MiniDDDDapperServerModule : ServerModuleBase
    {
        readonly MiniDDDDapperOptions _options;
        public MiniDDDDapperServerModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(MiniDDDDapperOptions).Name).Get<MiniDDDDapperOptions>();
        }

        public override void DoServiceRegister(ContainerBuilder serviceContainerBuilder)
        {
            if (_options != null && _options.Enable)
            {
                DbContextOptions dbContextOptions = new DbContextOptions
                {
                    ConnectionString = _options.ConnectionString,
                    DbType = _options.DbType
                };

                serviceContainerBuilder.RegisterType<UnitOfWork>()
                    .WithParameter("options", dbContextOptions)
                    .AsImplementedInterfaces()
                    .InstancePerLifetimeScope();

                // register repository
                var assembies = AppDomain.CurrentDomain.GetAssemblies();
                var repositories = assembies.SelectMany(x => x.GetTypes()).Where(x =>
                {
                    return x.IsClass && !x.IsAbstract && x.GetInterface(typeof(IRepository<,>).FullName) != null;
                }).ToList();
                repositories.ForEach(x => serviceContainerBuilder.RegisterType(x).AsImplementedInterfaces().InstancePerLifetimeScope());

            }
            base.DoServiceRegister(serviceContainerBuilder);
        }

    }
}

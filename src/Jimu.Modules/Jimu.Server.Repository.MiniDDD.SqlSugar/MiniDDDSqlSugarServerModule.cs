﻿using Autofac;
using Jimu.Module;
using Microsoft.Extensions.Configuration;
using MiniDDD;
using MiniDDD.UnitOfWork;
using MiniDDD.UnitOfWork.SqlSugar;
using System;
using System.Linq;

namespace Jimu.Server.Repository.MiniDDD.SqlSugar
{
    public class MiniDDDSqlSugarServerModule : ServerModuleBase
    {
        readonly MiniDDDSqlSugarOptions _options;
        IContainer _container = null;
        public MiniDDDSqlSugarServerModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(MiniDDDSqlSugarOptions).Name).Get<MiniDDDSqlSugarOptions>();
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
                Action<string> logAction = null;
                if (_options.OpenLogTrace)
                {
                    logAction = (log) =>
                    {
                        if (_container != null && _container.IsRegistered<Jimu.Logger.ILogger>())
                        {
                            var loggerFactory = _container.Resolve<ILoggerFactory>();
                            var logger = loggerFactory.Create(this.GetType());
                            logger.Info($"【SqlSugar】 - {log}");
                        }
                    };
                }

                serviceContainerBuilder.RegisterType<UnitOfWork>()
                    .WithParameter("options", dbContextOptions)
                    .WithParameter("logAction", logAction)
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

        public override void DoInit(IContainer container)
        {
            _container = container;
            base.DoServiceInit(container);
        }
    }
}

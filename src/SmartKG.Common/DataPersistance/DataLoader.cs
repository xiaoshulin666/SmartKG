﻿using Microsoft.Extensions.Configuration;
using Serilog;
using SmartKG.Common.Data;
using SmartKG.Common.Data.Configuration;
using SmartKG.Common.Data.KG;
using SmartKG.Common.Data.LU;
using SmartKG.Common.Data.Visulization;
using SmartKG.Common.DataPersistance;
using SmartKG.Common.DataStore;
using SmartKG.Common.Logger;
using SmartKG.Common.Parser.DataPersistance;
using System;
using System.Collections.Generic;

namespace SmartKG.Common.DataPersistence
{    
    public class DataLoader
    {
        private static DataLoader uniqueInstance;
    
        private ILogger log = Log.Logger.ForContext<DataLoader>();

        private static PersistanceType persistanceType;
        private static FilePathConfig filePathConfig;
        private static string connectionString;
        private static string defaultDBName;

        private List<Vertex> vList;
        private List<Edge> eList;
        private List<VisulizationConfig> vcList;

        private List<NLUIntentRule> iList;
        private List<EntityData> enList;
        private List<EntityAttributeData> eaList;

        private IDataAccessor dataAccessor;
        private List<ScenarioSetting> settings;



        public static DataLoader GetInstance()
        {
            return uniqueInstance;
        }


        public static DataLoader initInstance(IConfiguration config)
        {
            if (uniqueInstance == null)
            {
                

                uniqueInstance = new DataLoader();

                persistanceType = (PersistanceType)Enum.Parse(typeof(PersistanceType), config.GetSection("PersistanceType").Value, true);

                if (persistanceType == PersistanceType.File)
                {
                    filePathConfig = config.GetSection("FileDataPath").Get<FilePathConfig>();                    
                    uniqueInstance.dataAccessor = new FileDataAccessor(filePathConfig.RootPath);
                }
                else
                {
                    connectionString = config.GetConnectionString("MongoDbConnection");
                    defaultDBName = config.GetConnectionString("DatabaseName");

                    uniqueInstance.dataAccessor = new MongoDataAccessor(connectionString);
                }                                    
            }

            return uniqueInstance;
        }

        public PersistanceType GetPersistanceType()
        {
            return persistanceType;
        }

        public void Load(IConfiguration config)
        {
            this.settings = config.GetSection("Scenarios").Get<List<ScenarioSetting>>();

            if (persistanceType == PersistanceType.File)
            {              
                uniqueInstance.Load(filePathConfig.DefaultDataStore);                                
            }
            else
            {                 
                uniqueInstance.Load(defaultDBName);                
            }
        }

        public void Load(string dsName)
        {
           
            (this.vList, this.eList, this.vcList, this.iList , this.enList, this.eaList ) = this.dataAccessor.Load(dsName);

            if (this.vcList == null || this.vList == null || this.eList == null)
            {
                throw new Exception("Cannot load KG data from persistance.");
            }

            DataPersistanceKGParser kgParser = new DataPersistanceKGParser(this.vList, this.eList, this.vcList);
            kgParser.ParseKG();

            log.Information("Knowledge Graph is parsed.");
            Console.WriteLine("Knowledge Graph is parsed.");

            if (this.iList == null || this.enList == null)
            {
                log.Here().Warning("No NLU Data loaded from persistence");
            }
            else
            {
                DataPersistanceNLUParser nluParser = new DataPersistanceNLUParser(this.iList, this.enList, this.eaList);
                nluParser.Parse();

                List<Vertex> roots = KnowledgeGraphStore.GetInstance().GetAllVertexes();
                nluParser.ParseScenarioSettings(this.settings, roots);

                log.Information("NLU materials is parsed.");
                Console.WriteLine("NLU materials is parsed.");
            }
        }
        

        public List<Vertex> GetVertexCollection()
        {
            return this.vList;
        }

        public List<Edge> GetEdgeCollection()
        {

            return this.eList;
        }

        public List<VisulizationConfig> GetVisulizationConfigs()
        {
            return this.vcList;
        }

        public List<NLUIntentRule> GetIntentCollection()
        {
            return iList;
        }

        public List<EntityData> GetEntityCollection()
        {
            return enList;
        }

        public List<EntityAttributeData> GetEntityAttributeCollection()
        {
            return eaList;
        }
    }
}
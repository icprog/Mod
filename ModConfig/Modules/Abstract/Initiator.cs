﻿using Mod.Configuration.Modules;
using Mod.Configuration.Properties;
using Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mod.Modules.Abstracts
{
    public abstract class Initiator: IInitiator, IConfigurable
    {
        private Guid uniqueId;

        #region constructors
        public Initiator()
        {
        }
        #endregion

        #region properties
        [Configure(DefaultValue = false)]
        public virtual bool AutoCleanup
        {
            get;
            set;
        }

        [Configure(DefaultValue = false)]
        public virtual bool AutoInitialize
        {
            get;
            set;
        }

        [Configure(DefaultValue = false)]
        public virtual bool IsInitialized
        {
            get;
            set;
        }

        [Configure(InitType = typeof(Guid))]
        public virtual Guid UniqueId
        {
            get { return uniqueId; }
            set { uniqueId = value; }
        }
        #endregion

        #region functions
        public virtual bool Cleanup()
        {
            if(IsInitialized)
            {
                //!TODO clean all properties 
                IsInitialized = false;
                return !IsInitialized;
            }

            return false;
        }

        public virtual bool Initialize()
        {
            if(IsInitialized && AutoCleanup)
                Cleanup();

            if(!IsInitialized)
            {
                Type type = this.GetType();
                PropertyInfo[] properties = type.GetProperties();
                if(InitializeProperties(properties.GetEnumerator(), true))
                {
                    if(InitializeProperties(properties.GetEnumerator()))
                        IsInitialized = true;
                }

                UniqueId = Guid.NewGuid();
                return IsInitialized;
            }

            return false;
        }

        protected virtual bool InitializeProperty(PropertyInfo property)
        {
            bool result = false;
            if(property != null)
            {
                Type propertyType = property.PropertyType;
                ConfigureAttribute configure = property.GetCustomAttribute(typeof(ConfigureAttribute)) as ConfigureAttribute;
                MethodInfo getMethod = property.GetGetMethod();
                object value = null;
                if((getMethod != null) && (getMethod.GetParameters().Count() == 0))
                    value = property.GetValue(this);

                if((configure == null) && (value != null))
                {
                    if((value as IInitiator) != null)
                        (value as IInitiator).Initialize();
                }
                else if((configure != null) && (value == null))
                {
                    if(configure.AutoInit())
                        if(configure.InitTypeTemplateCnt > 0)
                            InitializeTemplateTypes(configure, property);
                        else
                            property.SetValue(this, Activator.CreateInstance(configure.InitType));
                    else
                        property.SetValue(this, configure.DefaultValue);
                    value = property.GetValue(this);
                }
                if((value as IInitiator) != null)
                    ((IInitiator)value).Initialize();
                result = true;
            }
            return result;
        }

        protected virtual bool InitializeProperties(System.Collections.IEnumerator enumerator, bool preInit = false)
        {
            bool result = true;
            while(enumerator.MoveNext() && result)
            {
                PropertyInfo current = enumerator.Current as PropertyInfo;
                ConfigureAttribute configAttribute = (current.GetCustomAttribute(typeof(ConfigureAttribute)) as ConfigureAttribute);
                if(preInit && (configAttribute != null) && configAttribute.PreInit)
                    result = InitializeProperty(enumerator.Current as PropertyInfo);
                else if(((!preInit) && (configAttribute == null)) || ((configAttribute != null) && (!configAttribute.PreInit)))
                    result = InitializeProperty(enumerator.Current as PropertyInfo);
            }
            return result;
        }

        protected virtual bool InitializeTemplateTypes(ConfigureAttribute config, PropertyInfo property)
        {
            bool result = false;
            if(this.GetType().GenericTypeArguments.Count() >= (config.InitTypeTemplateCnt - config.InitTypeParameters.Count()))
            {
                int i = 0;
                while(config.InitTypeParameters.Count() < config.InitTypeTemplateCnt)
                {
                    config.InitTypeParameters.Add(this.GetType().GenericTypeArguments[i]);
                    i++;
                }
                if(config.InitType.IsGenericTypeDefinition)
                    config.InitType = config.InitType.MakeGenericType(config.InitTypeParameters.ToArray());
                property.SetValue(this, Activator.CreateInstance(config.InitType));
                result = true;
            }
            return result;
        }

        #endregion

        public virtual ModuleConfig ToConfig(bool inDepth = false)
        {
            if(IsInitialized)
            {
                ModuleConfig result = new ModuleConfig();
                result.Type = this.GetType().AssemblyQualifiedName;
                result.Instance = this;
                PropertyInfo[] properties = this.GetType().GetProperties();

                for(int i = 0; i < properties.Count(); i++)
                {
                    ConfigureAttribute configAttribute = (properties[i].GetCustomAttribute(typeof(ConfigureAttribute)) as ConfigureAttribute);
                    if(configAttribute != null)
                    {
                        var propertyConfig = new PropertyConfig();
                        result.PropertyConfigCollection[result.PropertyConfigCollection.Count] = propertyConfig;
                        propertyConfig.Name = properties[i].Name;
                        object value = properties[i].GetValue(this);
                        value = properties[i].GetValue(this);
                        if(value != null)
                        {
                            propertyConfig.Type = value.GetType().FullName;
                            if((!value.GetType().IsClass) || (value.GetType() == typeof(string)))
                            {
                                propertyConfig.Value = value.ToString();
                            }
                            else if(inDepth)
                            {
                                IConfigurable configurable = value as IConfigurable;
                                if(configurable != null)
                                {
                                    ModuleConfig moduleConfig = configurable.ToConfig();
                                    moduleConfig.Instance = value;
                                    result.ModuleConfigCollection[result.ModuleConfigCollection.Count] = moduleConfig;
                                }
                            }
                        }
                    }
                }
                return result;
            }
            return null;
        }

        public virtual bool FromConfig(ModuleConfig config)
        {
            throw new NotImplementedException();
        }
    }
}

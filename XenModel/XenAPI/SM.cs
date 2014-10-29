/*
 * Copyright (c) Citrix Systems, Inc.
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 *   1) Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 * 
 *   2) Redistributions in binary form must reproduce the above
 *      copyright notice, this list of conditions and the following
 *      disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
 * COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using System.Collections;
using System.Collections.Generic;

using CookComputing.XmlRpc;


namespace XenAPI
{
    /// <summary>
    /// A storage manager plugin
    /// First published in XenServer 4.0.
    /// </summary>
    public partial class SM : XenObject<SM>
    {
        public SM()
        {
        }

        public SM(string uuid,
            string name_label,
            string name_description,
            string type,
            string vendor,
            string copyright,
            string version,
            string required_api_version,
            Dictionary<string, string> configuration,
            string[] capabilities,
            Dictionary<string, long> features,
            Dictionary<string, string> other_config,
            string driver_filename)
        {
            this.uuid = uuid;
            this.name_label = name_label;
            this.name_description = name_description;
            this.type = type;
            this.vendor = vendor;
            this.copyright = copyright;
            this.version = version;
            this.required_api_version = required_api_version;
            this.configuration = configuration;
            this.capabilities = capabilities;
            this.features = features;
            this.other_config = other_config;
            this.driver_filename = driver_filename;
        }

        /// <summary>
        /// Creates a new SM from a Proxy_SM.
        /// </summary>
        /// <param name="proxy"></param>
        public SM(Proxy_SM proxy)
        {
            this.UpdateFromProxy(proxy);
        }

        public override void UpdateFrom(SM update)
        {
            uuid = update.uuid;
            name_label = update.name_label;
            name_description = update.name_description;
            type = update.type;
            vendor = update.vendor;
            copyright = update.copyright;
            version = update.version;
            required_api_version = update.required_api_version;
            configuration = update.configuration;
            capabilities = update.capabilities;
            features = update.features;
            other_config = update.other_config;
            driver_filename = update.driver_filename;
        }

        internal void UpdateFromProxy(Proxy_SM proxy)
        {
            uuid = proxy.uuid == null ? null : (string)proxy.uuid;
            name_label = proxy.name_label == null ? null : (string)proxy.name_label;
            name_description = proxy.name_description == null ? null : (string)proxy.name_description;
            type = proxy.type == null ? null : (string)proxy.type;
            vendor = proxy.vendor == null ? null : (string)proxy.vendor;
            copyright = proxy.copyright == null ? null : (string)proxy.copyright;
            version = proxy.version == null ? null : (string)proxy.version;
            required_api_version = proxy.required_api_version == null ? null : (string)proxy.required_api_version;
            configuration = proxy.configuration == null ? null : Maps.convert_from_proxy_string_string(proxy.configuration);
            capabilities = proxy.capabilities == null ? new string[] {} : (string [])proxy.capabilities;
            features = proxy.features == null ? null : Maps.convert_from_proxy_string_long(proxy.features);
            other_config = proxy.other_config == null ? null : Maps.convert_from_proxy_string_string(proxy.other_config);
            driver_filename = proxy.driver_filename == null ? null : (string)proxy.driver_filename;
        }

        public Proxy_SM ToProxy()
        {
            Proxy_SM result_ = new Proxy_SM();
            result_.uuid = (uuid != null) ? uuid : "";
            result_.name_label = (name_label != null) ? name_label : "";
            result_.name_description = (name_description != null) ? name_description : "";
            result_.type = (type != null) ? type : "";
            result_.vendor = (vendor != null) ? vendor : "";
            result_.copyright = (copyright != null) ? copyright : "";
            result_.version = (version != null) ? version : "";
            result_.required_api_version = (required_api_version != null) ? required_api_version : "";
            result_.configuration = Maps.convert_to_proxy_string_string(configuration);
            result_.capabilities = capabilities;
            result_.features = Maps.convert_to_proxy_string_long(features);
            result_.other_config = Maps.convert_to_proxy_string_string(other_config);
            result_.driver_filename = (driver_filename != null) ? driver_filename : "";
            return result_;
        }

        /// <summary>
        /// Creates a new SM from a Hashtable.
        /// </summary>
        /// <param name="table"></param>
        public SM(Hashtable table)
        {
            uuid = Marshalling.ParseString(table, "uuid");
            name_label = Marshalling.ParseString(table, "name_label");
            name_description = Marshalling.ParseString(table, "name_description");
            type = Marshalling.ParseString(table, "type");
            vendor = Marshalling.ParseString(table, "vendor");
            copyright = Marshalling.ParseString(table, "copyright");
            version = Marshalling.ParseString(table, "version");
            required_api_version = Marshalling.ParseString(table, "required_api_version");
            configuration = Maps.convert_from_proxy_string_string(Marshalling.ParseHashTable(table, "configuration"));
            capabilities = Marshalling.ParseStringArray(table, "capabilities");
            features = Maps.convert_from_proxy_string_long(Marshalling.ParseHashTable(table, "features"));
            other_config = Maps.convert_from_proxy_string_string(Marshalling.ParseHashTable(table, "other_config"));
            driver_filename = Marshalling.ParseString(table, "driver_filename");
        }

        public bool DeepEquals(SM other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Helper.AreEqual2(this._uuid, other._uuid) &&
                Helper.AreEqual2(this._name_label, other._name_label) &&
                Helper.AreEqual2(this._name_description, other._name_description) &&
                Helper.AreEqual2(this._type, other._type) &&
                Helper.AreEqual2(this._vendor, other._vendor) &&
                Helper.AreEqual2(this._copyright, other._copyright) &&
                Helper.AreEqual2(this._version, other._version) &&
                Helper.AreEqual2(this._required_api_version, other._required_api_version) &&
                Helper.AreEqual2(this._configuration, other._configuration) &&
                Helper.AreEqual2(this._capabilities, other._capabilities) &&
                Helper.AreEqual2(this._features, other._features) &&
                Helper.AreEqual2(this._other_config, other._other_config) &&
                Helper.AreEqual2(this._driver_filename, other._driver_filename);
        }

        public override string SaveChanges(Session session, string opaqueRef, SM server)
        {
            if (opaqueRef == null)
            {
                System.Diagnostics.Debug.Assert(false, "Cannot create instances of this type on the server");
                return "";
            }
            else
            {
                if (!Helper.AreEqual2(_other_config, server._other_config))
                {
                    SM.set_other_config(session, opaqueRef, _other_config);
                }

                return null;
            }
        }
        /// <summary>
        /// Get a record containing the current state of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static SM get_record(Session session, string _sm)
        {
            return new SM((Proxy_SM)session.proxy.sm_get_record(session.uuid, (_sm != null) ? _sm : "").parse());
        }

        /// <summary>
        /// Get a reference to the SM instance with the specified UUID.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_uuid">UUID of object to return</param>
        public static XenRef<SM> get_by_uuid(Session session, string _uuid)
        {
            return XenRef<SM>.Create(session.proxy.sm_get_by_uuid(session.uuid, (_uuid != null) ? _uuid : "").parse());
        }

        /// <summary>
        /// Get all the SM instances with the given label.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_label">label of object to return</param>
        public static List<XenRef<SM>> get_by_name_label(Session session, string _label)
        {
            return XenRef<SM>.Create(session.proxy.sm_get_by_name_label(session.uuid, (_label != null) ? _label : "").parse());
        }

        /// <summary>
        /// Get the uuid field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_uuid(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_uuid(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the name/label field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_name_label(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_name_label(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the name/description field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_name_description(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_name_description(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the type field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_type(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_type(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the vendor field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_vendor(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_vendor(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the copyright field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_copyright(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_copyright(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the version field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_version(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_version(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the required_api_version field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_required_api_version(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_required_api_version(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the configuration field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static Dictionary<string, string> get_configuration(Session session, string _sm)
        {
            return Maps.convert_from_proxy_string_string(session.proxy.sm_get_configuration(session.uuid, (_sm != null) ? _sm : "").parse());
        }

        /// <summary>
        /// Get the capabilities field of the given SM.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string[] get_capabilities(Session session, string _sm)
        {
            return (string [])session.proxy.sm_get_capabilities(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Get the features field of the given SM.
        /// First published in XenServer 6.2.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static Dictionary<string, long> get_features(Session session, string _sm)
        {
            return Maps.convert_from_proxy_string_long(session.proxy.sm_get_features(session.uuid, (_sm != null) ? _sm : "").parse());
        }

        /// <summary>
        /// Get the other_config field of the given SM.
        /// First published in XenServer 4.1.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static Dictionary<string, string> get_other_config(Session session, string _sm)
        {
            return Maps.convert_from_proxy_string_string(session.proxy.sm_get_other_config(session.uuid, (_sm != null) ? _sm : "").parse());
        }

        /// <summary>
        /// Get the driver_filename field of the given SM.
        /// First published in XenServer 5.0.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        public static string get_driver_filename(Session session, string _sm)
        {
            return (string)session.proxy.sm_get_driver_filename(session.uuid, (_sm != null) ? _sm : "").parse();
        }

        /// <summary>
        /// Set the other_config field of the given SM.
        /// First published in XenServer 4.1.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        /// <param name="_other_config">New value to set</param>
        public static void set_other_config(Session session, string _sm, Dictionary<string, string> _other_config)
        {
            session.proxy.sm_set_other_config(session.uuid, (_sm != null) ? _sm : "", Maps.convert_to_proxy_string_string(_other_config)).parse();
        }

        /// <summary>
        /// Add the given key-value pair to the other_config field of the given SM.
        /// First published in XenServer 4.1.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        /// <param name="_key">Key to add</param>
        /// <param name="_value">Value to add</param>
        public static void add_to_other_config(Session session, string _sm, string _key, string _value)
        {
            session.proxy.sm_add_to_other_config(session.uuid, (_sm != null) ? _sm : "", (_key != null) ? _key : "", (_value != null) ? _value : "").parse();
        }

        /// <summary>
        /// Remove the given key and its corresponding value from the other_config field of the given SM.  If the key is not in that Map, then do nothing.
        /// First published in XenServer 4.1.
        /// </summary>
        /// <param name="session">The session</param>
        /// <param name="_sm">The opaque_ref of the given sm</param>
        /// <param name="_key">Key to remove</param>
        public static void remove_from_other_config(Session session, string _sm, string _key)
        {
            session.proxy.sm_remove_from_other_config(session.uuid, (_sm != null) ? _sm : "", (_key != null) ? _key : "").parse();
        }

        /// <summary>
        /// Return a list of all the SMs known to the system.
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        public static List<XenRef<SM>> get_all(Session session)
        {
            return XenRef<SM>.Create(session.proxy.sm_get_all(session.uuid).parse());
        }

        /// <summary>
        /// Get all the SM Records at once, in a single XML RPC call
        /// First published in XenServer 4.0.
        /// </summary>
        /// <param name="session">The session</param>
        public static Dictionary<XenRef<SM>, SM> get_all_records(Session session)
        {
            return XenRef<SM>.Create<Proxy_SM>(session.proxy.sm_get_all_records(session.uuid).parse());
        }

        /// <summary>
        /// Unique identifier/object reference
        /// </summary>
        public virtual string uuid
        {
            get { return _uuid; }
            set
            {
                if (!Helper.AreEqual(value, _uuid))
                {
                    _uuid = value;
                    Changed = true;
                    NotifyPropertyChanged("uuid");
                }
            }
        }
        private string _uuid;

        /// <summary>
        /// a human-readable name
        /// </summary>
        public virtual string name_label
        {
            get { return _name_label; }
            set
            {
                if (!Helper.AreEqual(value, _name_label))
                {
                    _name_label = value;
                    Changed = true;
                    NotifyPropertyChanged("name_label");
                }
            }
        }
        private string _name_label;

        /// <summary>
        /// a notes field containing human-readable description
        /// </summary>
        public virtual string name_description
        {
            get { return _name_description; }
            set
            {
                if (!Helper.AreEqual(value, _name_description))
                {
                    _name_description = value;
                    Changed = true;
                    NotifyPropertyChanged("name_description");
                }
            }
        }
        private string _name_description;

        /// <summary>
        /// SR.type
        /// </summary>
        public virtual string type
        {
            get { return _type; }
            set
            {
                if (!Helper.AreEqual(value, _type))
                {
                    _type = value;
                    Changed = true;
                    NotifyPropertyChanged("type");
                }
            }
        }
        private string _type;

        /// <summary>
        /// Vendor who created this plugin
        /// </summary>
        public virtual string vendor
        {
            get { return _vendor; }
            set
            {
                if (!Helper.AreEqual(value, _vendor))
                {
                    _vendor = value;
                    Changed = true;
                    NotifyPropertyChanged("vendor");
                }
            }
        }
        private string _vendor;

        /// <summary>
        /// Entity which owns the copyright of this plugin
        /// </summary>
        public virtual string copyright
        {
            get { return _copyright; }
            set
            {
                if (!Helper.AreEqual(value, _copyright))
                {
                    _copyright = value;
                    Changed = true;
                    NotifyPropertyChanged("copyright");
                }
            }
        }
        private string _copyright;

        /// <summary>
        /// Version of the plugin
        /// </summary>
        public virtual string version
        {
            get { return _version; }
            set
            {
                if (!Helper.AreEqual(value, _version))
                {
                    _version = value;
                    Changed = true;
                    NotifyPropertyChanged("version");
                }
            }
        }
        private string _version;

        /// <summary>
        /// Minimum SM API version required on the server
        /// </summary>
        public virtual string required_api_version
        {
            get { return _required_api_version; }
            set
            {
                if (!Helper.AreEqual(value, _required_api_version))
                {
                    _required_api_version = value;
                    Changed = true;
                    NotifyPropertyChanged("required_api_version");
                }
            }
        }
        private string _required_api_version;

        /// <summary>
        /// names and descriptions of device config keys
        /// </summary>
        public virtual Dictionary<string, string> configuration
        {
            get { return _configuration; }
            set
            {
                if (!Helper.AreEqual(value, _configuration))
                {
                    _configuration = value;
                    Changed = true;
                    NotifyPropertyChanged("configuration");
                }
            }
        }
        private Dictionary<string, string> _configuration;

        /// <summary>
        /// capabilities of the SM plugin
        /// </summary>
        public virtual string[] capabilities
        {
            get { return _capabilities; }
            set
            {
                if (!Helper.AreEqual(value, _capabilities))
                {
                    _capabilities = value;
                    Changed = true;
                    NotifyPropertyChanged("capabilities");
                }
            }
        }
        private string[] _capabilities;

        /// <summary>
        /// capabilities of the SM plugin, with capability version numbers
        /// First published in XenServer 6.2.
        /// </summary>
        public virtual Dictionary<string, long> features
        {
            get { return _features; }
            set
            {
                if (!Helper.AreEqual(value, _features))
                {
                    _features = value;
                    Changed = true;
                    NotifyPropertyChanged("features");
                }
            }
        }
        private Dictionary<string, long> _features;

        /// <summary>
        /// additional configuration
        /// First published in XenServer 4.1.
        /// </summary>
        public virtual Dictionary<string, string> other_config
        {
            get { return _other_config; }
            set
            {
                if (!Helper.AreEqual(value, _other_config))
                {
                    _other_config = value;
                    Changed = true;
                    NotifyPropertyChanged("other_config");
                }
            }
        }
        private Dictionary<string, string> _other_config;

        /// <summary>
        /// filename of the storage driver
        /// First published in XenServer 5.0.
        /// </summary>
        public virtual string driver_filename
        {
            get { return _driver_filename; }
            set
            {
                if (!Helper.AreEqual(value, _driver_filename))
                {
                    _driver_filename = value;
                    Changed = true;
                    NotifyPropertyChanged("driver_filename");
                }
            }
        }
        private string _driver_filename;
    }
}

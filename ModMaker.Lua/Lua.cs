<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using ModMaker.Lua.Parser;
using ModMaker.Lua.Runtime;

namespace ModMaker.Lua
{
    /// <summary>
    /// The main manager of a Lua state. Manages lua chunks,
    /// the environments, and registering types and
    /// global methods.
    /// </summary>
    [LuaIgnore]
    public sealed class Lua : IDisposable
    {
        bool _disposed = false;
        LuaSettings _settings;
        LuaEnvironment _E;
        List<LuaChunk> _chunks;

        /// <summary>
        /// Creates a new Lua object with default settings.
        /// </summary>
        public Lua()
            : this(new LuaSettings()) { }
        /// <summary>
        /// Creates a new Lua object with specified settings.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        public Lua(LuaSettings settings)
        {
            if (settings.Stdin == null)
                settings.Stdin = Console.OpenStandardInput();
            if (settings.Stdout == null)
                settings.Stdout = Console.OpenStandardOutput();
            if (settings.Libraries == LuaLibraries.UseDefaults)
                settings.Libraries = LuaLibraries.All;
            if (settings.ModuleBinder == null)
                settings.ModuleBinder = new ModuleBinder(this);

            this._chunks = new List<LuaChunk>();
            this._E = new LuaEnvironment(settings);
        }
        /// <summary>
        /// Disposes the Lua object.
        /// </summary>
        ~Lua()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Gets the settings for the current environment.  Cannot be changed after initialization.
        /// </summary>
        public LuaSettings Settings { get { return _E.Settings; } }
        /// <summary>
        /// Gets or sets the global environment.  Value cannot be changed while
        /// parsing.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">When trying to set the environment to null.</exception>
        public LuaEnvironment Environment 
        {
            get { return _E; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                lock (this)
                    _E = value;
            }
        }
        /// <summary>
        /// Gets the chunk at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index in the order they were loaded.</param>
        /// <returns>The lua chunk at that index.</returns>
        public LuaChunk this[int index] { get { return _chunks[index]; } }

        /// <summary>
        /// Registers a delegate for use with this Lua object.
        /// </summary>
        /// <param name="func">The delegate to register, cannot be null.</param>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        /// <exception cref="System.ArgumentNullException">When func is null.</exception>
        /// <exception cref="System.MulticastNotSupportedException">When func has more than one item in the InvokationList.</exception>
        public void Register(Delegate func)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            if (func.GetInvocationList().Length > 1)
                throw new MulticastNotSupportedException("Cannot register a multicast delegate, must register each individually.");

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterDelegate(func, func.Method.Name);
            }
        }
        /// <summary>
        /// Registers a delegate with the given name for use with this Lua object.
        /// </summary>
        /// <param name="func">The delegate to register, cannot be null.</param>
        /// <param name="name">The name of the delegate.  If null, will use the function name.</param>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        /// <exception cref="System.ArgumentNullException">When func is null.</exception>
        /// <exception cref="System.MulticastNotSupportedException">When func has more than one item in the InvokationList.</exception>
        public void Register(Delegate func, string name)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            if (func.GetInvocationList().Length > 1)
                throw new MulticastNotSupportedException("Cannot register a multicast delegate, must register each individually.");
            if (name == null)
                name = func.Method.Name;

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterDelegate(func, name);
            }
        }
        /// <summary>
        /// Registers a type for use within Lua.
        /// </summary>
        /// <param name="type">The type to register, cannot be null.</param>
        /// <exception cref="System.ArgumentNullException">When type is null.</exception>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        public void Register(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.GetCustomAttributes(typeof(LuaIgnoreAttribute), false).Length > 0)
                throw new ArgumentException("Cannot register a type that is marked with LuaIgnoreAttribute.");

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterType(type, type.Name);
            }
        }

        /// <summary>
        /// Executes all the chunks in order.
        /// </summary>
        /// <param name="args">The arguments to pass to each chunk.</param>
        /// <returns>The union of the results of each chunk.</returns>
        public object[] Execute(params object[] args)
        {
            lock (this)
            {
                List<object> ret = new List<object>();
                foreach (var item in _chunks)
                {
                    ret.AddRange(item.Execute(args));
                }
                return ret.ToArray();
            }
        }
        /// <summary>
        /// Executes one of the chunks.
        /// </summary>
        /// <param name="args">The arguments to the chunk.</param>
        /// <param name="index">The index of the loaded chunk.</param>
        /// <returns>The results of the chunk.</returns>
        public object[] Execute(int index, params object[] args)
        {
            lock (this)
            {
                if (index >= _chunks.Count || index < 0)
                    throw new IndexOutOfRangeException("The index must be positive and less than the number of chunks loaded.");
                return _chunks[index].Execute(args);
            }
        }

        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), Path.GetFileNameWithoutExtension(path));

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path, bool @override)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), Path.GetFileNameWithoutExtension(path), @override);

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), name);

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path, string name, bool @override)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), name, @override);

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd());

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream, string name)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), name);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream, bool @override)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), force: @override);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream, string name, bool @override)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c.ReadToEnd(), name, @override);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk)
        {
            lock (this)
            {
                LuaChunk ret = PlainParser.LoadChunk(Environment, chunk);

                if (!_chunks.Contains(ret))
                    _chunks.Add(ret);
                return ret;
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk, string name)
        {
            lock (this)
            {
                LuaChunk ret = PlainParser.LoadChunk(Environment, chunk, name);

                if (!_chunks.Contains(ret))
                    _chunks.Add(ret);
                return ret;
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk, bool @override)
        {
            lock (this)
            {
                LuaChunk ret = PlainParser.LoadChunk(Environment, chunk, force: @override);

                if (!_chunks.Contains(ret))
                    _chunks.Add(ret);
                return ret;
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <param name="override">True to load the file even if it is in the cache, otherwise false.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk, string name, bool @override)
        {
            lock (this)
            {
                LuaChunk ret = PlainParser.LoadChunk(Environment, chunk, name, @override);

                if (!_chunks.Contains(ret))
                    _chunks.Add(ret);
                return ret;
            }
        }

        /// <summary>
        /// Loads and executes the file at the path specified.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The values returned from the file.</returns>
        public object[] DoFile(string path, params object[] args)
        {
            var ret = Load(path);
            return ret.Execute(args);
        }
        /// <summary>
        /// Loads and executes the specified text.
        /// </summary>
        /// <param name="chunk">The chunk to execute.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The values returned from the file.</returns>
        public object[] DoText(string chunk, params object[] args)
        {
            var ret = LoadText(chunk);
            return ret.Execute(args);
        }

        /// <summary>
        /// Disposes the Lua object and any object it created.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                }

                _chunks = null;
                _E = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static dynamic GetVariable(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(new LuaSettings());
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return E[name];
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static dynamic GetVariable(LuaSettings settings, string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(settings);
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return E[name];
        }
        /// <summary>
        /// Gets variables from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="names">The names of the variables to get.</param>
        /// <returns>The value of the variables.</returns>
        public static dynamic[] GetVariables(string path, params string[] names)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(new LuaSettings());
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return names.Select(s => E[s]).ToArray();
        }
        /// <summary>
        /// Gets variables from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="names">The names of the variables to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variables.</returns>
        public static dynamic[] GetVariables(LuaSettings settings, string path, params string[] names)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(settings);
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return names.Select(s => E[s]).ToArray();
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static T GetVariable<T>(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(new LuaSettings());
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return (T)RuntimeHelper.ConvertType((object)E[name], typeof(T));
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static T GetVariable<T>(string path, string name, LuaSettings settings)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            LuaEnvironment E = new LuaEnvironment(settings);
            LuaChunk ret = PlainParser.LoadChunk(E, File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            ret.Execute();
            return (T)RuntimeHelper.ConvertType((object)E[name], typeof(T));
        }
    }
=======
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using ModMaker.Lua.Parser;
using ModMaker.Lua.Runtime;

namespace ModMaker.Lua
{
    /// <summary>
    /// The main manager of a Lua state. Manages lua chunks,
    /// the environments, and registering types and
    /// global methods.
    /// </summary>
    public sealed class Lua : IDisposable
    {
        bool _disposed = false;
        LuaSettings _settings;
        LuaEnvironment _E;
        List<LuaChunk> _chunks;

        /// <summary>
        /// Creates a new Lua object with default settings.
        /// </summary>
        public Lua()
            : this(new LuaSettings()) { }
        /// <summary>
        /// Creates a new Lua object with specified settings.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        public Lua(LuaSettings settings)
        {
            if (settings.Stdin == null)
                settings.Stdin = Console.OpenStandardInput();
            if (settings.Stdout == null)
                settings.Stdout = Console.OpenStandardOutput();
            if (settings.Libraries == LuaLibraries.UseDefaults)
                settings.Libraries = LuaLibraries.All;
            if (settings.ModuleBinder == null)
                settings.ModuleBinder = new ModuleBinder(this);

            this._chunks = new List<LuaChunk>();
            this._E = new LuaEnvironment(settings);
        }
        /// <summary>
        /// Disposes the Lua object.
        /// </summary>
        ~Lua()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Gets the settings for the current environment.  Cannot be changed after initialization.
        /// </summary>
        public LuaSettings Settings { get { return _E.Settings; } }
        /// <summary>
        /// Gets or sets the global environment.  Value cannot be changed while
        /// parsing.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">When trying to set the environment to null.</exception>
        public LuaEnvironment Environment 
        {
            get { return _E; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                lock (this)
                    _E = value;
            }
        }
        /// <summary>
        /// Gets the chunk at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index in the order they were loaded.</param>
        /// <returns>The lua chunk at that index.</returns>
        public LuaChunk this[int index] { get { return _chunks[index]; } }

        /// <summary>
        /// Registers a delegate for use with this Lua object.
        /// </summary>
        /// <param name="func">The delegate to register, cannot be null.</param>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        /// <exception cref="System.ArgumentNullException">When func is null.</exception>
        /// <exception cref="System.MulticastNotSupportedException">When func has more than one item in the InvokationList.</exception>
        public void Register(Delegate func)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            if (func.GetInvocationList().Length > 1)
                throw new MulticastNotSupportedException("Cannot register a multicast delegate, must register each individually.");

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterDelegate(func, func.Method.Name);
            }
        }
        /// <summary>
        /// Registers a delegate with the given name for use with this Lua object.
        /// </summary>
        /// <param name="func">The delegate to register, cannot be null.</param>
        /// <param name="name">The name of the delegate.  If null, will use the function name.</param>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        /// <exception cref="System.ArgumentNullException">When func is null.</exception>
        /// <exception cref="System.MulticastNotSupportedException">When func has more than one item in the InvokationList.</exception>
        public void Register(Delegate func, string name)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            if (func.GetInvocationList().Length > 1)
                throw new MulticastNotSupportedException("Cannot register a multicast delegate, must register each individually.");
            if (name == null)
                name = func.Method.Name;

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterDelegate(func, name);
            }
        }
        /// <summary>
        /// Registers a type for use within Lua.
        /// </summary>
        /// <param name="type">The type to register, cannot be null.</param>
        /// <exception cref="System.ArgumentNullException">When type is null.</exception>
        /// <exception cref="System.ArgumentException">When there is another method or type with the same name registered.</exception>
        public void Register(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.GetCustomAttributes(typeof(LuaIgnoreAttribute), false).Length > 0)
                throw new ArgumentException("Cannot register a type that is marked with LuaIgnoreAttribute.");

            lock (this)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_settings.Name ?? "ModMaker.Lua.Lua");

                _E.RegisterType(type, type.Name);
            }
        }

        /// <summary>
        /// Executes all the chunks in order.
        /// </summary>
        /// <param name="args">The arguments to pass to each chunk.</param>
        /// <returns>The union of the results of each chunk.</returns>
        public object[] Execute(params object[] args)
        {
            lock (this)
            {
                List<object> ret = new List<object>();
                foreach (var item in _chunks)
                {
                    ret.AddRange(item.Execute(args));
                }
                return ret.ToArray();
            }
        }
        /// <summary>
        /// Executes one of the chunks.
        /// </summary>
        /// <param name="args">The arguments to the chunk.</param>
        /// <param name="index">The index of the loaded chunk.</param>
        /// <returns>The results of the chunk.</returns>
        public object[] Execute(int index, params object[] args)
        {
            lock (this)
            {
                if (index >= _chunks.Count || index < 0)
                    throw new IndexOutOfRangeException("The index must be positive and less than the number of chunks loaded.");
                return _chunks[index].Execute(args);
            }
        }

        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        var hash = SHA512.Create().ComputeHash(fs);
                        fs.Position = 0;
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c, hash, Path.GetFileNameWithoutExtension(path));

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        [SecuritySafeCritical]
        public LuaChunk Load(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                lock (this)
                {
                    using (TextReader c = new StreamReader(fs))
                    {
                        var hash = SHA512.Create().ComputeHash(fs);                        
                        fs.Position = 0;
                        LuaChunk ret = PlainParser.LoadChunk(Environment, c, hash, name);

                        if (!_chunks.Contains(ret))
                            _chunks.Add(ret);
                        return ret;
                    }
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    byte[] hash = null;
                    if (stream.CanSeek)
                    {
                        long pos = stream.Position;
                        hash = SHA512.Create().ComputeHash(stream);
                        stream.Position = pos;
                    }

                    LuaChunk ret = PlainParser.LoadChunk(Environment, c, hash);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the script from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk Load(Stream stream, string name)
        {
            lock (this)
            {
                using (TextReader c = new StreamReader(stream))
                {
                    byte[] hash = null;
                    if (stream.CanSeek)
                    {
                        long pos = stream.Position;
                        hash = SHA512.Create().ComputeHash(stream);
                        stream.Position = pos;
                    }

                    LuaChunk ret = PlainParser.LoadChunk(Environment, c, hash, name);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk)
        {
            lock (this)
            {
                using (TextReader c = new StringReader(chunk))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c, SHA512.Create().ComputeHash(Encoding.Unicode.GetBytes(chunk)));

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }
        /// <summary>
        /// Loads a LuaChunk from a pre-loaded string.
        /// </summary>
        /// <param name="chunk">The Lua script to load from.</param>
        /// <param name="name">The name to give the chunk.</param>
        /// <returns>The loaded chunk.</returns>
        public LuaChunk LoadText(string chunk, string name)
        {
            lock (this)
            {
                using (TextReader c = new StringReader(chunk))
                {
                    LuaChunk ret = PlainParser.LoadChunk(Environment, c, SHA512.Create().ComputeHash(Encoding.Unicode.GetBytes(chunk)), name);

                    if (!_chunks.Contains(ret))
                        _chunks.Add(ret);
                    return ret;
                }
            }
        }

        /// <summary>
        /// Loads and executes the file at the path specified.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The values returned from the file.</returns>
        public object[] DoFile(string path, params object[] args)
        {
            var ret = Load(path);
            return ret.Execute(args);
        }
        /// <summary>
        /// Loads and executes the specified text.
        /// </summary>
        /// <param name="chunk">The chunk to execute.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The values returned from the file.</returns>
        public object[] DoText(string chunk, params object[] args)
        {
            var ret = LoadText(chunk);
            return ret.Execute(args);
        }

        /// <summary>
        /// Disposes the Lua object and any object it created.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                }

                _chunks = null;
                _E = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static dynamic GetVariable(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(new LuaSettings());
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return E[name];
                }
            }
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static dynamic GetVariable(LuaSettings settings, string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(settings);
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return E[name];
                }
            }
        }
        /// <summary>
        /// Gets variables from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="names">The names of the variables to get.</param>
        /// <returns>The value of the variables.</returns>
        public static dynamic[] GetVariables(string path, params string[] names)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(new LuaSettings());
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return names.Select(s => E[s]).ToArray();
                }
            }
        }
        /// <summary>
        /// Gets variables from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="names">The names of the variables to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variables.</returns>
        public static dynamic[] GetVariables(LuaSettings settings, string path, params string[] names)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(settings);
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return names.Select(s => E[s]).ToArray();
                }
            }
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static T GetVariable<T>(string path, string name)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(new LuaSettings());
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return RuntimeHelper.ConvertType(E[name], typeof(T));
                }
            }
        }
        /// <summary>
        /// Gets a variable from a given Lua file.
        /// </summary>
        /// <param name="path">The path to the Lua file.</param>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="settings">The settings used to load the chunk.</param>
        /// <returns>The value of the variable or null if not found.</returns>
        public static T GetVariable<T>(string path, string name, LuaSettings settings)
        {
            path = Path.GetFullPath(path);
            new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (TextReader c = new StreamReader(fs))
                {
                    LuaEnvironment E = new LuaEnvironment(settings);
                    LuaChunk ret = PlainParser.LoadChunk(E, c, SHA512.Create().ComputeHash(fs), Path.GetFileNameWithoutExtension(path));
                    ret.Execute();
                    return RuntimeHelper.ConvertType(E[name], typeof(T));
                }
            }
        }
    }
>>>>>>> ca31a2f4607b904d0d7876c07b13afac67d2736e
}
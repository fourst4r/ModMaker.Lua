<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using ModMaker.Lua.Runtime;
using System.Reflection;

namespace ModMaker.Lua.Parser
{
    class FuncCallItem : IParseItem
    {
        List<IParseItem> _args;
        string instance;

        public FuncCallItem(IParseItem prefix, string instance = null)
        {
            if (!prefix.Type.HasFlag(ParseType.PrefixExp))
                throw new ArgumentException("The prefix of a function call must be a PrefixExp.");

            this.Prefix = prefix;
            this._args = new List<IParseItem>();
            this.instance = instance;
        }

        public IParseItem Prefix { get; private set; }
        public ParseType Type { get { return ParseType.PrefixExp | ParseType.Statement | ParseType.Expression; } }
        public bool TailCall { get; set; }
        public bool Statement { get; set; }

        public void AddItem(IParseItem item)
        {
            if (!item.Type.HasFlag(ParseType.Expression))
                throw new ArgumentException("Parameter to FunCall must be an Expression.");

            _args.Add(item);
        }
        public void GenerateIL(ChunkBuilderNew eb)
        {
            /* load the args into an array */
            ILGenerator gen = eb.CurrentGenerator;
            LocalBuilder f = gen.DeclareLocal(typeof(object));

            // only need to create args array if there are aruments
            if (_args.Count > 0 || instance != null)
            {
                // args = new List<object>();
                LocalBuilder args = gen.DeclareLocal(typeof(List<object>));
                gen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(new Type[0]));
                gen.Emit(OpCodes.Stloc, args);

                /* add 'self' if instance call */
                if (instance != null)
                {
                    // f = {Prefix};
                    Prefix.GenerateIL(eb);
                    gen.Emit(OpCodes.Stloc, f);

                    // args.Add(f);
                    gen.Emit(OpCodes.Ldloc, args);
                    gen.Emit(OpCodes.Ldloc, f);
                    gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add"));

                    // f = RuntimeHelper.Indexer({E}, f, {instance});
                    eb.PushEnv();
                    gen.Emit(OpCodes.Ldloc, f);
                    gen.Emit(OpCodes.Ldstr, instance);
                    gen.Emit(OpCodes.Call, typeof(RuntimeHelper).GetMethod("Indexer"));
                    gen.Emit(OpCodes.Stloc, f);
                }
                else
                {
                    // f = {Prefix};
                    Prefix.GenerateIL(eb);
                    gen.Emit(OpCodes.Stloc, f);
                }

                foreach (var item in _args)
                {
                    // args.Add({item});
                    gen.Emit(OpCodes.Ldloc, args);
                    item.GenerateIL(eb);
                    gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add"));
                }

                // RumtimeHelper.Invoke({_E}, f, args.ToArray());
                eb.PushEnv();
                gen.Emit(OpCodes.Ldloc, f);
                gen.Emit(OpCodes.Ldloc, args);
                gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("ToArray"));
            }
            else
            {
                // RumtimeHelper.Invoke({_E}, {Prefix}, null);
                eb.PushEnv();
                Prefix.GenerateIL(eb);
                gen.Emit(OpCodes.Ldnull);
            }

            if (TailCall)
                gen.Emit(OpCodes.Tailcall);
            gen.Emit(OpCodes.Call, typeof(RuntimeHelper).GetMethod("Invoke"));

            if (Statement)
                gen.Emit(OpCodes.Pop);
        }
        public void ResolveLabels(ChunkBuilderNew cb, LabelTree tree)
        {
            foreach (var item in _args)
                item.ResolveLabels(cb, tree);
            Prefix.ResolveLabels(cb, tree);
        }
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using ModMaker.Lua.Runtime;
using System.Reflection;

namespace ModMaker.Lua.Parser.Items
{
    class FuncCallItem : IParseItem
    {
        List<IParseItem> _args;
        string instance;

        public FuncCallItem(IParseItem prefix, string instance = null)
        {
            if (!prefix.Type.HasFlag(ParseType.PrefixExp))
                throw new ArgumentException("The prefix of a function call must be a PrefixExp.");

            this.Prefix = prefix;
            this._args = new List<IParseItem>();
            this.instance = instance;
        }

        public IParseItem Prefix { get; private set; }
        public ParseType Type { get { return ParseType.PrefixExp | ParseType.Statement | ParseType.Expression; } }
        public bool TailCall { get; set; }
        public bool Statement { get; set; }

        public void AddItem(IParseItem item)
        {
            if (!item.Type.HasFlag(ParseType.Expression))
                throw new ArgumentException("Parameter to FunCall must be an Expression.");

            _args.Add(item);
        }
        public void GenerateIL(ChunkBuilderNew eb)
        {
            /* load the args into an array */
            ILGenerator gen = eb.CurrentGenerator;
            LocalBuilder f = gen.DeclareLocal(typeof(object));

            // only need to create args array if there are aruments
            if (_args.Count > 0 || instance != null)
            {
                // args = new List<object>();
                LocalBuilder args = gen.DeclareLocal(typeof(List<object>));
                gen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(new Type[0]));
                gen.Emit(OpCodes.Stloc, args);

                /* add 'self' if instance call */
                if (instance != null)
                {
                    // f = {Prefix};
                    Prefix.GenerateIL(eb);
                    gen.Emit(OpCodes.Stloc, f);

                    // args.Add(f);
                    gen.Emit(OpCodes.Ldloc, args);
                    gen.Emit(OpCodes.Ldloc, f);
                    gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add"));

                    // f = RuntimeHelper.Indexer({E}, f, {instance});
                    eb.PushEnv();
                    gen.Emit(OpCodes.Ldloc, f);
                    gen.Emit(OpCodes.Ldstr, instance);
                    gen.Emit(OpCodes.Call, typeof(RuntimeHelper).GetMethod("Indexer"));
                    gen.Emit(OpCodes.Stloc, f);
                }
                else
                {
                    // f = {Prefix};
                    Prefix.GenerateIL(eb);
                    gen.Emit(OpCodes.Stloc, f);
                }

                foreach (var item in _args)
                {
                    // args.Add({item});
                    gen.Emit(OpCodes.Ldloc, args);
                    item.GenerateIL(eb);
                    gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add"));
                }

                // RumtimeHelper.Invoke({_E}, f, args.ToArray());
                eb.PushEnv();
                gen.Emit(OpCodes.Ldloc, f);
                gen.Emit(OpCodes.Ldloc, args);
                gen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("ToArray"));
            }
            else
            {
                // RumtimeHelper.Invoke({_E}, f, null);
                eb.PushEnv();
                gen.Emit(OpCodes.Ldloc, f);
                gen.Emit(OpCodes.Ldnull);
            }

            if (TailCall)
                gen.Emit(OpCodes.Tailcall);
            gen.Emit(OpCodes.Call, typeof(RuntimeHelper).GetMethod("Invoke"));

            if (Statement)
                gen.Emit(OpCodes.Pop);
        }
        public void ResolveLabels(ChunkBuilderNew cb, LabelTree tree)
        {
            foreach (var item in _args)
                item.ResolveLabels(cb, tree);
            Prefix.ResolveLabels(cb, tree);
        }
    }
}
>>>>>>> ca31a2f4607b904d0d7876c07b13afac67d2736e

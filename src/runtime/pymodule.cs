using System;

namespace Python.Runtime
{
    public class PyModule : PyScope
    {
        internal PyModule(ref NewReference reference) : base(ref reference, PyScopeManager.Global) { }
        public PyModule(PyObject o) : base(o.Reference, PyScopeManager.Global) { }

        /// <summary>
        /// Given a module or package name, import the
        /// module and return the resulting module object as a <see cref="PyModule"/>.
        /// </summary>
        /// <param name="name">Fully-qualified module or package name</param>
        public static PyModule Import(string name)
        {
            NewReference op = Runtime.PyImport_ImportModule(name);
            PythonException.ThrowIfIsNull(op);
            return new PyModule(ref op);
        }

        /// <summary>
        /// Reloads the module, and returns the updated object
        /// </summary>
        public PyModule Reload()
        {
            NewReference op = Runtime.PyImport_ReloadModule(this.Reference);
            PythonException.ThrowIfIsNull(op);
            return new PyModule(ref op);
        }

        public static PyModule FromString(string name, string code)
        {
            using NewReference c = Runtime.Py_CompileString(code, "none", (int)RunFlagType.File);
            PythonException.ThrowIfIsNull(c);
            NewReference m = Runtime.PyImport_ExecCodeModule(name, c);
            PythonException.ThrowIfIsNull(m);
            return new PyModule(ref m);
        }

        public static bool Exists(string name)
        {
            // first check if there is an existing module
            BorrowedReference modules = Runtime.PyImport_GetModuleDict();
            BorrowedReference m = Runtime.PyDict_GetItemString(modules, name);
            return !m.IsNull;
        }

        public static PyModule Create(string name, string filename = "none")
        {
            if (Exists(name)) return null;
            BorrowedReference modules = Runtime.PyImport_GetModuleDict();
            // create a new module
            var op = Runtime.PyModule_New(name);
            PythonException.ThrowIfIsNull(op);

            // setup the module basics (__builtins__ and __file__)
            BorrowedReference globals = Runtime.PyModule_GetDict(new BorrowedReference(op.DangerousGetAddress()));
            PythonException.ThrowIfIsNull(globals);
            BorrowedReference __builtins__ = Runtime.PyEval_GetBuiltins();
            PythonException.ThrowIfIsNull(__builtins__);
            int rc = Runtime.PyDict_SetItemString(globals, "__builtins__", __builtins__);
            PythonException.ThrowIfIsNotZero(rc);
            rc = Runtime.PyDict_SetItemString(globals, "__file__",
                new BorrowedReference(filename.ToPython().Handle));
            PythonException.ThrowIfIsNotZero(rc);

            // add to sys.modules
            rc = Runtime.PyDict_SetItemString(modules, name, op);
            PythonException.ThrowIfIsNotZero(rc);

            return new PyModule(ref op);
        }
    }
}

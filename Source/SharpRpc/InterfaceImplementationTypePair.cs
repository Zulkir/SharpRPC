using System;

namespace SharpRpc
{
    public struct InterfaceImplementationTypePair
    {
        public Type Interface;
        public Type ImplementationType;

        public InterfaceImplementationTypePair(Type serviceInterface, Type implementationType)
        {
            Interface = serviceInterface;
            ImplementationType = implementationType;
        }
    }
}
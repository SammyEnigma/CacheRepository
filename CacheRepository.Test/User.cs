using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CacheRepository.Tests
{
    public class User
    {
        public int Id;
        public string Name;
        public short Age;
        public int Level;
    }

    public class Car
    {
        private int _id;
        private string _name;
        private int _color;
        private float _weight;

        private volatile int _version;

        public int ID
        {
            set
            {
                this._id = value;
                Interlocked.Increment(ref _version);
            }
            get
            {
                return this._id;
            }
        }

        public string Name
        {
            set
            {
                this._name = value;
                Interlocked.Increment(ref _version);
            }
            get
            {
                return this._name;
            }
        }


        public int Color
        {
            set
            {
                this._color = value;
                Interlocked.Increment(ref _version);
            }
            get
            {
                return this._color;
            }
        }

        public float Weight
        {
            set
            {
                this._weight = value;
                Interlocked.Increment(ref _version);
            }
            get
            {
                return this._weight;
            }
        }

        public override int GetHashCode()
        {
            return _version;
        }
    }
}

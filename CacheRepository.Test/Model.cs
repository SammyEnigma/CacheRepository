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

    // 说明：
    // 这里Order跟User即是经典的一个join关系，为了方便这里定义为一对一的关系
    public class Order
    {
        public int Id;
        public string SerialNum;
        public decimal Price;
        public User User;
    }

    // 说明：
    // 这里Teacher跟Student之间的join关系是一对多的关系，多个学生之间共享一个老师；
    // 虽然Teacher里面没有List<Student>，但是Student对象持有一个Techaer对象的引用
    public class Teacher
    {
        public int Id;
        public string Name;
        public string Phone;
    }

    public class Student
    {
        public int Id;
        public string Name;
        public Teacher Teacher;
    }
}

using System;
using System.Collections.Generic;

namespace CacheRepository.Tests
{
    public class JoinedRepository1 : CacheRepository<int, Order, int>
    {
        public override Func<int, (int index, string tag)> GetShardingRule()
        {
            return p => (0, "默认分片");
        }

        public override int GetShardKey(Order value)
        {
            return 0;
        }

        protected override int GetKey(Order value)
        {
            return value.Id;
        }

        // 说明：
        // 执行base.Init()会间接调用GetRawData()方法。这里order跟user是一对一的关系，
        // 可以直接在GetRawData()方法中通过dapper来返回这种join对象；
        protected override List<Order> GetRawData()
        {
            return new List<Order>
            {
                new Order
                {
                    Id = 1,
                    Price = 100,
                    SerialNum = Guid.NewGuid().ToString(),
                    User = new User { Id = 1, Name = "a", Age = 10, Level = 1 }
                }
            };
        }
    }

    public class JoinedRepository2 : CacheRepository<int, Student, int>
    {
        private Dictionary<int, Teacher> _teacher_infos;
        public JoinedRepository2()
        {
            _teacher_infos = new Dictionary<int, Teacher>();
        }

        public override Func<int, (int index, string tag)> GetShardingRule()
        {
            return p => (0, "默认分片");
        }

        public override int GetShardKey(Student value)
        {
            return 0;
        }

        protected override int GetKey(Student value)
        {
            return value.Id;
        }

        // 说明：
        // 执行base.Init()会间接调用GetRawData()方法。这里teacher跟student是一对多的关系，
        // 业务逻辑上可能会要求所有student对象引用到一个teacher对象上，这样teacher的某个字段更新
        // 了的话比如这里的phone字段，就可以即时的反馈到student对象上（因为是引用类型，完美的join）
        protected override List<Student> GetRawData()
        {
            var ret = new List<Student>();
            var teachers = GetAllTeacher();
            foreach (var teacher in teachers)
            {
                var st_list = GetStudentBy(teacher);
                ret.AddRange(st_list);
            }
            return ret;
        }

        private List<Student> GetStudentBy(Teacher teacher)
        {
            // 说明：
            // 这里使用dapper从数据库中读出来的student数据，是不能通过dapper的mapping直接包含
            // teacher的，因为我们想要的效果就是student可以共用一个teacher对象。所以将teacher
            // 先行读取出来，然后在student创建的时候手动绑定好二者的关系（也就是直接赋值）
            if (teacher.Id == 1)
            {
                return new List<Student>
                {
                    new Student { Id = 1, Name = "S_a", Teacher = teacher },
                    new Student { Id = 2, Name = "S_b", Teacher = teacher },
                };
            }
            else
            {
                return new List<Student>
                {
                    new Student { Id = 3, Name = "S_c", Teacher = teacher },
                };
            }
        }

        private List<Teacher> GetAllTeacher()
        {
            var ret = new List<Teacher>();

            var t1 = new Teacher { Id = 1, Name = "T_a", Phone = "123" };
            _teacher_infos.Add(t1.Id, t1);
            ret.Add(t1);

            var t2 = new Teacher { Id = 2, Name = "T_b", Phone = "124" };
            _teacher_infos.Add(t2.Id, t2);
            ret.Add(t2);

            return ret;
        }
    }
}

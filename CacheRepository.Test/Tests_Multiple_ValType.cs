using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Multiple_ValType
    {
        private readonly MultipleShardsRepository _repository;

        public Tests_Multiple_ValType()
        {
            _repository = new MultipleShardsRepository();
            _repository.Init();
        }
    }
}

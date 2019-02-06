using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Multiple
    {
        private readonly MultipleShardsRepository _repository;

        public Tests_Multiple()
        {
            _repository = new MultipleShardsRepository();
            _repository.Init();
        }
    }
}

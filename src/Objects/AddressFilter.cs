using System.Collections;
using System.Reflection;
using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects {
    public sealed class AddressFilter {
        public static readonly IEnumerable<BaseFilter> Filters;
        static AddressFilter()
        {
            var filters = new FilterCollection();
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(BaseFilter)))
                .OrderByDescending(type => type.GetCustomAttribute<AddressFilterAttribute>()?.Priority ?? -1);
            foreach (Type type in types)
            {
                if (Activator.CreateInstance(type) is BaseFilter filter)
                    filters.Add(filter);
            }

            AddressFilter.Filters = filters;
        }

        /// <summary>
        /// A base class for checking whether an Email Address should be filtered out
        /// Instances of <see cref="BaseFilter"/> are created automatically using Reflection when the program starts
        /// A priority for the Filter can be applied using a <see cref="AddressFilterAttribute"/>
        /// </summary>
        public abstract class BaseFilter {
            /// <summary>A Name for the Filter, which is added to the Debug Stack when run</summary>
            public abstract string Name { get; }

            public virtual Result ValidateEmailAddress(ref EmailAddress address)
                => Result.CONTINUE;

            public virtual ValueTask<Result> ValidateEmailAddressAsync(ref EmailAddress address, CancellationToken cancellation = default)
                => ValueTask.FromResult(this.ValidateEmailAddress(ref address));

            /// <summary>Convert a <see cref="bool"/> to a <see cref="Result"/>, CONTINUE when true, or DENY when false</summary>
            protected Result Continue(bool success)
                => success ? Result.CONTINUE : Result.DENY;
        }

        private sealed class FilterCollection : IEnumerable<BaseFilter> {
            /// <summary>Use a List so that we maintain entry order</summary>
            private readonly IList<BaseFilter> Filters = new List<BaseFilter>();

            public void Add(BaseFilter filter)
                => this.Filters.Add(filter);

            /// <inheritdoc />
            public IEnumerator<BaseFilter> GetEnumerator()
                => this.Filters.GetEnumerator();

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
                => this.GetEnumerator();
        }
    }
}

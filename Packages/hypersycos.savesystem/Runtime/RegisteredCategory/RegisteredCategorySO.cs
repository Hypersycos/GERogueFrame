using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "Registered Category", menuName = "SaveSystem/Registered Category", order = 1)]
    public class RegisteredCategorySO : RegisteredCategorySOBase
    {
        public RegisteredCategory MyCategory { get; private set; }
        [SerializeField] List<RegisteredValueSOBase> _Values = new();
        [SerializeField] List<RegisteredCategorySOBase> _SubCategories = new();

        public override IReadOnlyList<RegisteredValueSOBase> Values => _Values.AsReadOnly();
        public override IReadOnlyList<RegisteredCategorySOBase> SubCategories => _SubCategories.AsReadOnly();

        public override RegisteredCategory MyObject => MyCategory ?? Create();

        public override RegisteredCategory Create()
        {
            List<RegisteredValue> values = _Values.Where(v => v is not null).Select(v => v.MyObject).ToList();
            List<RegisteredCategory> categories = _SubCategories.Where(v => v is not null).Select(c => c.MyObject ?? c.Create()).ToList();

            MyCategory = new(_IsEphemeral, MappedSerializers.ToList(), name, null, values, categories);
            return MyCategory;
        }

        protected internal override void Clear()
        {
            MyCategory = null;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "Registered File", menuName = "SaveSystem/Registered File", order = 2)]
    public class RegisteredFileSO : RegisteredFileSOBase
    {
        [SerializeField] string Path;
        [SerializeField] List<RegisteredCategorySOBase> _Categories = new ();
        [SerializeField] bool AllowUnregisteredValues = false;

        public override IReadOnlyList<RegisteredCategorySOBase> Categories => _Categories.AsReadOnly();
        public RegisteredFile MyFile { get; protected set; }

        public override RegisteredFile MyObject => MyFile ?? Create(); 

        protected internal override void Clear()
        {
            MyFile = null;
        }

        public override RegisteredFile Create()
        {
            List<RegisteredCategory> categories = _Categories.Where(v => v is not null).Select(c => c.MyObject ?? c.Create()).ToList();
            if (AllowUnregisteredValues)
                MyFile = new FileWithStore(_IsEphemeral, MappedSerializers.ToList(), name, Path, categories);
            else
                MyFile = new(_IsEphemeral, MappedSerializers.ToList(), name, Path, categories);
            return MyFile;
        }
    }
}

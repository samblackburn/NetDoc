namespace NetDoc.TestData
{
    public class Zzzzz
    {
        public int Field;
        private void CallerOfField() => Field = 1;

        public int Property { get; set; }
        private int  CallerOfGetter() => Property;
        private void CallerOfSetter() => Property = 1;

        public int Method() => 1;
        private void CallerOfMethod() => Method();

        public int this[int i]
        {
            get => 1;
            set { }
        }
        private int CallerOfIndexGetter() => this[2];
        private int CallerOfIndexSetter() => this[2] = 1;
    }
}

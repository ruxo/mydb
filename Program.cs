using System;
using RZ.App;
using RZ.Foundation;
using static System.Console;

int PersonIdComparer(Person lhs, Person rhs) => Math.Sign(lhs.Id - rhs.Id);

var tree = new MemoryBTree<Person>(PersonIdComparer);
char select = '\0';

while ((select = Menu()) != 'Q') {
    switch (select) {
        case '1':
            tree.Add(InputPerson());
            break;
        case '2':
            tree.Traverse().Iter(WriteLine);
            break;
    }
}

static char Menu() {
    WriteLine("Select:");
    WriteLine("1. Add a person");
    WriteLine("2. List persons");
    WriteLine("3. Search a person by name");
    WriteLine("4. Search persons by age");
    WriteLine("Q. Quit");
    return char.ToUpper(ReadKey().KeyChar).SideEffect(_ => WriteLine());
}

Person InputPerson() {
    WriteLine();
    Write("ID number: ");
    var id = int.Parse(ReadLine()!);
    Write("Name: ");
    var name = ReadLine()!;
    Write("Age: ");
    var age = int.Parse(ReadLine()!);
    return new(id, name, age);
}
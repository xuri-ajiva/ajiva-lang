## The Ajiva-Lang Specification

basic desctrption

### File formats

Ajiva Sorce Code are [UTF-8](https://en.wikipedia.org/wiki/UTF-8) files with the `.ajc` file extension.
The Sorce Files are compiled with the `ajivac` compiler into `.alib`.

### Basic concept

The Ajiva use the concept context diffrent meaning for characters.
e.g.
```c
a < 10 // assing a to 10
if a < 20 // comparison
//[......] is named an expression
    a < 10 < 2 // assing a to be 10 < 2 witch is 20
// [..........]  <-- is considert a section
```

### Comments

Comments are `c` style.
Line Comments begin with `//` and block comments start with `/*` and must end in `*/`

```c
// This is a line Comment

/*
And this is a Block comment
You can write multiple lines in thair
*/
```

### Naming
A name is only allowed if every part canot be interpreted as any other statment.
e.g.
```c
1 // interpeded as number 1
1d // interpreted as number 1 but as double presition
1a // valid name
a j i v a // valid name 'a j i v a'

a if a // not valid because if is a control flow statment
1 < 2 // assings 2 to the valiable 1 but not usefull beacause you cant use variable 1 anywhere because its interpeted as 1
a < 1 // will set a to 1 not to 2 bacause 1 cal be inerpreted as the number 1

```

### Variables

Variables are staticly typed but not annotated meaning the copiler desides whitch type the vairable has.
Uses the Standart Naming spesification


#### Assingment

Ajiva uses the `<` left side assing or `>` right side assing charater to assing value

```c
a < 10 // set a to 10
"hello" > b // set b to "hello"
```

### Control flow

Control flow can be done by many diffrent statments

| statment | expects                | meaning                                                     |
| -------- | ---------------------- | ----------------------------------------------------------- |
| if       | expression --> section | checks the expression and branches into the section if true |
| while    | expression --> section | loops the given block as long as the expression is true     |

### Functions

### User Defined Structures

### Attributes

Attributes start with `@` or `#` or `^`

e.g
```cs
@entry // defines entry point
#pure  // wrap next block in a pure function named after the file, one allowed per file
```

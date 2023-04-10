native void Log(i32 i)
native void Log(str s)
@entry(100)
fn void FizzBuzz(i32 n) {
    for (i32 i = 0 i < n i = i + 1) {
        if ((i % 3) == 0) {
            if ((i % 5) == 0) {
                Log("FizzBuzz")
            } else {
                Log("Fizz")
            }
        } else if ((i % 5) == 0) {
            Log("Buzz")
        } else {
            Log(i)
        }
    }
}

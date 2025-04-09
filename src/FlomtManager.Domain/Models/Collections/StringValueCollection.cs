namespace FlomtManager.Domain.Models.Collections;

public class StringValueCollection
{
    public DateTime DateTime { get; set; }
    public string DateTimeString { get; set; }
    public string[] Values { get; set; }
    public int Count => Values.Length;
}

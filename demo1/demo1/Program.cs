using System;

class Comodity {
    public string Name { set; get; }
    private float price;
    private string type;
    private string description;
    //属性 
    public int number;
    //字段
    private int num;
    //属性封装
    public int Num { set; get; }
}


class Seller
{
    private string name;
    private float ballance;
    private Comodity[] comodities;
}
class customer { 
    private string name;
    private float ballance;
    private Comodity[] ShopingCart;
}

class ServiceProvider { 
     private Seller[] sellers;
    private Comodity[] allComodities;
    public Comodity[] searchComoditys (String index) {
        Comodity[] result = new Comodity[10];
        int j = 0;
        for (int i = 0; i < result.Length; i++) {
            if (allComodities[i].Name.Contains(index)) {
                result[j++] = allComodities[i];
            }
        }
        return result;
    }
}

class program {
    public static void Main(String[] args) {
        Console.WriteLine("test");
    }
} 
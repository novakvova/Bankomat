// See https://aka.ms/new-console-template for more information
using MyPrivate.Data;
using MyPrivate.Data.Entitys;

Console.WriteLine("Почав роботу із БД!");
var context = new ContextATM();

UserEntity user = new UserEntity
{
    FirstName = "Іван",
    LastName = "Іванов",
    FatherName = "Іванович",
    CardNumber = 1234567890123456,
    PinCode = 1234
};
context.Users.Add(user);
context.SaveChanges();
BalanceEntity balance = new BalanceEntity
{
    UserId = 1,
    Amount = 1000.00m
};

context.Balances.Add(balance);
context.SaveChanges();


using Newtonsoft.Json;
using StackExchange.Redis;
using System;

namespace MyPublisher {

class Program {

    private static ISubscriber _publisher = null;
    private static ISubscriber _subscriber = null;

    static void Main(string[] args) {
        System.Console.WriteLine("Card Reader STARTED");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
        _publisher = redis.GetSubscriber();

        System.Console.WriteLine("Press I to simulate card insert");
        System.Console.WriteLine("Press O to simulate card eject");
        System.Console.WriteLine("Press J to simulate ticket insert");
        System.Console.WriteLine("Press K to simulate ticket eject");
        System.Console.WriteLine("Press A to simulate MONTH ticket insert");
        System.Console.WriteLine("Press S to simulate MONTH ticket eject");
        System.Console.WriteLine("Press N to simulate month pensioner ticket insert");
        System.Console.WriteLine("Press M to simulate month pensioner ticket eject");
        System.Console.WriteLine("Press Escape to exit");

        CardInfo card = new CardInfo();
        card.cardType = 10;
        card.cardId = "1111-2222-3333-4455";
        card.cardNum = "12345678800";
        card.dateStart = 0;
        card.dateEnd = 0;

        TicketInfo ticket = new TicketInfo();
        ticket.cardType = 0;
        ticket.cardId = "1111-2222-3333-4440";
        ticket.cardNum = "12345670";
        ticket.dateStart = 1;// 1440578851;
        ticket.dateEnd = 0;// 1440578851;
        ticket.tripsLeft = 4;

        // Month ticket full priced
        TicketInfo monthTicket = new TicketInfo();
        monthTicket.cardType = 1;
        monthTicket.cardId = "1111-2222-3333-4441";
        monthTicket.cardNum = "12345671";
        monthTicket.dateStart = 1;// 1440578851;
        monthTicket.dateEnd = 0;// 1440578851;

        // Month ticket for pensioner
        TicketInfo monthTicketPension = new TicketInfo();
        monthTicketPension.cardType = 2;
        monthTicketPension.cardId = "1111-2222-3333-4442";
        monthTicketPension.cardNum = "12345672";
        monthTicketPension.dateStart = 1;// 1440578851;
        monthTicketPension.dateEnd = 0;// 1440578851;

        _subscriber = redis.GetSubscriber();
        _subscriber.Subscribe("ui2cardReader", (channel, message) => {
            //Console.WriteLine("received: " + (string)message);
            dynamic jsonDe = JsonConvert.DeserializeObject((string)message);
            Console.WriteLine("received command: " + jsonDe.cmd);
            Console.WriteLine("cmd ID: " + jsonDe.cmdID);
            Console.WriteLine("params: " + JsonConvert.SerializeObject(jsonDe.parameters));

            if (jsonDe.cmd == "activate-ticket" && card.cardType != 10) {
                ticket.dateStart = jsonDe.parameters.start;
                ticket.dateEnd = jsonDe.parameters.end;
                send2ui("cmdSuccess", ticket);
            }
            else if (jsonDe.cmd == "activate-ticket" && card.cardType == 10) { // ESEK activation
                card.dateStart = jsonDe.parameters.start;
                card.dateEnd = jsonDe.parameters.end;
                send2ui("cmdSuccess", card);
            }
        });

        ConsoleKeyInfo cki;
        while (true) {
            cki = Console.ReadKey();
            System.Console.WriteLine("");
            if (cki.Key == ConsoleKey.Escape) {
                System.Console.WriteLine("EXIT");
                break;
            }
            else if (cki.Key == ConsoleKey.I) {
                informCardInsert(card);
            }
            else if (cki.Key == ConsoleKey.O) {
                informCardEject(card);
            }
            else if (cki.Key == ConsoleKey.J) {
                informCardInsert(ticket);
            }
            else if (cki.Key == ConsoleKey.K) {
                informCardEject(ticket);
            }
            else if (cki.Key == ConsoleKey.A) {
                informCardInsert(monthTicket);
            }
            else if (cki.Key == ConsoleKey.S) {
                informCardEject(monthTicket);
            }
            else if (cki.Key == ConsoleKey.N) {
                informCardInsert(monthTicketPension);
            }
            else if (cki.Key == ConsoleKey.M) {
                informCardEject(monthTicketPension);
            }
        }
    }

    private static void informCardInsert(Object card) {
        if (card is CardInfo) {
            System.Console.WriteLine("CARD INSERT");
        }
        else if (card is TicketInfo) {
            System.Console.WriteLine("TICKET INSERT");
        }
        send2ui("inserted", card);
    }

    private static void informCardEject(Object card) {
        if (card is CardInfo) {
            System.Console.WriteLine("CARD EJECTED");
        }
        else if (card is TicketInfo) {
            System.Console.WriteLine("TICKET EJECTED");
        }
        send2ui("ejected", card);
    }

    private static void send2ui(string subject, Object data) {
        Msg msg = new Msg();
        msg.subject = subject;
        msg.data = data;
        string json = JsonConvert.SerializeObject(msg);
        //System.Console.WriteLine(json);
        _publisher.Publish("cardReader2ui", json);
    }

}

class CardInfo {
    public string cardId { get; set; }
    public int cardType { get; set; }
    public string cardNum { get; set; }
    public int dateStart { get; set; }
    public int dateEnd { get; set; }
}

class TicketInfo {
    public int cardType { get; set; }
    public string cardNum { get; set; }
    public string cardId { get; set; }
    public int dateStart { get; set; }
    public int dateEnd { get; set; }
    public int tripsLeft { get; set; }
}

class Msg {
    public string subject { get; set; }
    public Object data { get; set; }
}

}

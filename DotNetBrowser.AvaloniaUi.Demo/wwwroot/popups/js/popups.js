function writeDate() {

    var now = new Date();
    var days = new Array('Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday');
    var months = new Array('January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December');
    var date = ((now.getDate() < 10) ? "0" : "") + now.getDate();

    function y2k(number) { return (number < 1000) ? number + 1900 : number; }

    today = "Popup  " + days[now.getDay()] + " " +
      months[now.getMonth()] + ", " +
      date + " " +
      (y2k(now.getYear()));

    if (document.all || document.getElementById) { // Browser Check
      document.title = today.toString();
    } else {
      self.status = today.toString(); // Default to status.
    }
  }
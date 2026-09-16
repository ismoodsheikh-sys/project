// VMS — shell interactions (theme, nav, tabs, drawers, menus, OTP, charts)
(function () {
  "use strict";

  var THEME_KEY = "fleetpulse-theme";
  var SIDEBAR_KEY = "fleetpulse-sidebar";
  var DENSITY_KEY = "fleetpulse-density";

  /* ---------------- Theme ---------------- */
  function applyTheme(mode) {
    var root = document.documentElement;
    if (mode === "system") { root.removeAttribute("data-theme"); }
    else { root.setAttribute("data-theme", mode); }
    document.querySelectorAll("[data-theme-btn]").forEach(function (btn) {
      btn.classList.toggle("active", btn.getAttribute("data-theme-btn") === mode);
    });
  }

  function initTheme() {
    var saved = localStorage.getItem(THEME_KEY) || "system";
    applyTheme(saved);
    document.querySelectorAll("[data-theme-btn]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var mode = btn.getAttribute("data-theme-btn");
        localStorage.setItem(THEME_KEY, mode);
        applyTheme(mode);
      });
    });
  }

  /* ---------------- Table density ---------------- */
  function applyDensity(mode) {
    document.body.classList.toggle("density-compact", mode === "compact");
    document.querySelectorAll("[data-density-btn]").forEach(function (btn) {
      btn.classList.toggle("active", btn.getAttribute("data-density-btn") === mode);
    });
  }

  function initDensity() {
    var saved = localStorage.getItem(DENSITY_KEY) || "comfortable";
    applyDensity(saved);
    document.querySelectorAll("[data-density-btn]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var mode = btn.getAttribute("data-density-btn");
        localStorage.setItem(DENSITY_KEY, mode);
        applyDensity(mode);
      });
    });
  }

  /* ---------------- Sidebar ---------------- */
  function initSidebar() {
    var body = document.body;
    if (localStorage.getItem(SIDEBAR_KEY) === "collapsed") {
      body.classList.add("sidebar-collapsed");
    }
    var collapseBtn = document.querySelector("[data-collapse-toggle]");
    if (collapseBtn) {
      collapseBtn.addEventListener("click", function () {
        body.classList.toggle("sidebar-collapsed");
        localStorage.setItem(SIDEBAR_KEY, body.classList.contains("sidebar-collapsed") ? "collapsed" : "expanded");
      });
    }
    var menuBtn = document.querySelector("[data-mobile-menu-toggle]");
    if (menuBtn) {
      menuBtn.addEventListener("click", function () { body.classList.toggle("sidebar-open"); });
    }
    document.addEventListener("click", function (e) {
      if (!body.classList.contains("sidebar-open")) return;
      var sidebar = document.querySelector(".sidebar");
      if (sidebar && !sidebar.contains(e.target) && !(menuBtn && menuBtn.contains(e.target))) {
        body.classList.remove("sidebar-open");
      }
    });
  }

  /* ---------------- Tabs ---------------- */
  function initTabs() {
    document.querySelectorAll("[data-tabs]").forEach(function (group) {
      var id = group.getAttribute("data-tabs");
      var panels = document.querySelectorAll('[data-tab-panel-group="' + id + '"]');
      group.querySelectorAll("[data-tab]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          group.querySelectorAll("[data-tab]").forEach(function (b) { b.classList.remove("active"); });
          btn.classList.add("active");
          var target = btn.getAttribute("data-tab");
          panels.forEach(function (p) {
            p.classList.toggle("active", p.getAttribute("data-tab-panel") === target);
          });
        });
      });
    });
  }

  /* ---------------- Dropdown menus ---------------- */
  function initDropdowns() {
    document.querySelectorAll("[data-dropdown-toggle]").forEach(function (btn) {
      var menu = document.getElementById(btn.getAttribute("data-dropdown-toggle"));
      if (!menu) return;
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var isOpen = menu.classList.contains("open");
        document.querySelectorAll(".dropdown-menu.open").forEach(function (m) { m.classList.remove("open"); });
        if (!isOpen) menu.classList.add("open");
      });
    });
    document.addEventListener("click", function () {
      document.querySelectorAll(".dropdown-menu.open").forEach(function (m) { m.classList.remove("open"); });
    });
  }

  /* ---------------- Drawers ---------------- */
  function initDrawers() {
    document.querySelectorAll("[data-drawer-open]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var drawer = document.getElementById(btn.getAttribute("data-drawer-open"));
        var overlay = document.querySelector("[data-drawer-overlay]");
        if (drawer) drawer.classList.add("open");
        if (overlay) overlay.classList.add("open");
      });
    });
    document.querySelectorAll("[data-drawer-close]").forEach(function (btn) {
      btn.addEventListener("click", closeAllDrawers);
    });
    var overlay = document.querySelector("[data-drawer-overlay]");
    if (overlay) overlay.addEventListener("click", closeAllDrawers);
  }
  function closeAllDrawers() {
    document.querySelectorAll(".drawer.open").forEach(function (d) { d.classList.remove("open"); });
    document.querySelectorAll(".drawer-overlay.open").forEach(function (o) { o.classList.remove("open"); });
  }

  /* ---------------- Modals ---------------- */
  function closeAllModals() {
    document.querySelectorAll(".modal.open").forEach(function (m) { m.classList.remove("open"); });
    document.querySelectorAll(".modal-overlay.open").forEach(function (o) { o.classList.remove("open"); });
  }
  function initModals() {
    document.querySelectorAll("[data-modal-close]").forEach(function (btn) {
      btn.addEventListener("click", closeAllModals);
    });
    var overlay = document.querySelector("[data-modal-overlay]");
    if (overlay) overlay.addEventListener("click", closeAllModals);
    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape") closeAllModals();
    });
  }

  /* ---------------- Invoice preview modal (Fuel management) ---------------- */
  function initInvoiceModal() {
    var modal = document.getElementById("modal-invoice");
    if (!modal) return;
    var overlay = document.querySelector("[data-modal-overlay]");
    var pdfLink = modal.querySelector("[data-invoice-pdf]");
    var excelLink = modal.querySelector("[data-invoice-excel]");
    var printBtn = modal.querySelector("[data-invoice-print]");

    function setText(field, value) {
      modal.querySelectorAll('[data-field="' + field + '"]').forEach(function (el) { el.textContent = value; });
    }

    document.querySelectorAll("[data-invoice-open]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var d = btn.dataset;
        setText("invoiceNo", d.invoiceNo);
        setText("vehicle", d.vehicle);
        setText("vehicleMakeModel", d.vehicleMakeModel);
        setText("driver", d.driver);
        setText("depot", d.depot);
        setText("invoiceDate", d.invoiceDate);
        setText("transactionDate", d.transactionDate);
        setText("paymentMethod", d.paymentMethod);
        setText("station", d.station);
        setText("description", d.description);
        setText("liters", d.liters);
        setText("costPerLiter", d.costPerLiter);
        setText("amount", d.amount);
        setText("subtotal", d.amount);
        setText("total", d.amount);
        setText("odometer", d.odometer);

        if (pdfLink) pdfLink.href = d.pdfUrl;
        if (excelLink) excelLink.href = d.excelUrl;

        modal.classList.add("open");
        if (overlay) overlay.classList.add("open");
      });
    });

    if (printBtn) printBtn.addEventListener("click", function () { window.print(); });
  }

  /* ---------------- Password visibility ---------------- */
  function initPasswordToggles() {
    document.querySelectorAll("[data-toggle-visibility]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var input = document.getElementById(btn.getAttribute("data-toggle-visibility"));
        if (!input) return;
        input.type = input.type === "password" ? "text" : "password";
        btn.classList.toggle("is-visible");
      });
    });
  }

  /* ---------------- OTP inputs ---------------- */
  function initOtp() {
    document.querySelectorAll("[data-otp-group]").forEach(function (group) {
      var boxes = Array.prototype.slice.call(group.querySelectorAll(".otp-box"));
      boxes.forEach(function (box, i) {
        box.addEventListener("input", function () {
          box.value = box.value.replace(/[^0-9]/g, "").slice(0, 1);
          if (box.value && boxes[i + 1]) boxes[i + 1].focus();
        });
        box.addEventListener("keydown", function (e) {
          if (e.key === "Backspace" && !box.value && boxes[i - 1]) boxes[i - 1].focus();
        });
        box.addEventListener("paste", function (e) {
          var data = (e.clipboardData || window.clipboardData).getData("text").replace(/[^0-9]/g, "");
          if (!data) return;
          e.preventDefault();
          data.split("").slice(0, boxes.length).forEach(function (ch, idx) { boxes[idx].value = ch; });
          var next = boxes[Math.min(data.length, boxes.length - 1)];
          if (next) next.focus();
        });
      });
    });
  }

  /* ---------------- Map zoom controls (Tracking) ---------------- */
  function initMapZoom() {
    document.querySelectorAll("[data-map-zoom-root]").forEach(function (root) {
      var canvas = root.querySelector("[data-map-canvas]");
      if (!canvas) return;
      var scale = 1;
      function apply() {
        canvas.style.transform = "scale(" + scale + ")";
      }
      var zoomIn = root.querySelector("[data-map-zoom-in]");
      var zoomOut = root.querySelector("[data-map-zoom-out]");
      var recenter = root.querySelector("[data-map-recenter]");
      if (zoomIn) zoomIn.addEventListener("click", function () { scale = Math.min(scale + 0.25, 2.5); apply(); });
      if (zoomOut) zoomOut.addEventListener("click", function () { scale = Math.max(scale - 0.25, 1); apply(); });
      if (recenter) recenter.addEventListener("click", function () { scale = 1; apply(); });
    });
  }

  /* ---------------- Chart hover tooltips ---------------- */
  function initChartTooltips() {
    document.querySelectorAll("[data-viz]").forEach(function (viz) {
      var tooltip = viz.querySelector(".viz-tooltip");
      if (!tooltip) return;
      viz.querySelectorAll("[data-tt]").forEach(function (mark) {
        mark.addEventListener("mouseenter", function (e) { showTip(e); });
        mark.addEventListener("mousemove", function (e) { showTip(e); });
        mark.addEventListener("mouseleave", function () { tooltip.classList.remove("show"); });
        mark.addEventListener("focus", function (e) { showTip(e, true); });
        mark.addEventListener("blur", function () { tooltip.classList.remove("show"); });
      });
      function showTip(e, isFocus) {
        var rect = viz.getBoundingClientRect();
        var mark = e.currentTarget;
        var mr = mark.getBoundingClientRect();
        var x = isFocus ? (mr.left + mr.width / 2 - rect.left) : (e.clientX - rect.left);
        var y = (isFocus ? mr.top : e.clientY) - rect.top;
        tooltip.style.left = x + "px";
        tooltip.style.top = (y - 10) + "px";
        tooltip.innerHTML = mark.getAttribute("data-tt");
        tooltip.classList.add("show");
      }
    });
  }

  /* ---------------- Range / meter sliders (settings) ---------------- */
  function initRangeFill() {
    document.querySelectorAll("input[type=range][data-fill]").forEach(function (input) {
      function update() {
        var pct = ((input.value - input.min) / (input.max - input.min)) * 100;
        input.style.background = "linear-gradient(to right, var(--brand-500) " + pct + "%, var(--border-strong) " + pct + "%)";
      }
      input.addEventListener("input", update);
      update();
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    initTheme();
    initDensity();
    initSidebar();
    initTabs();
    initDropdowns();
    initDrawers();
    initModals();
    initInvoiceModal();
    initPasswordToggles();
    initOtp();
    initMapZoom();
    initChartTooltips();
    initRangeFill();
  });
})();

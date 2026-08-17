import os
import re
import json
import uuid
import datetime
from typing import Optional, List, Dict, Any

from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import requests
import pyodbc

app = FastAPI(title="WeddingApp AI Concierge API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Supported SQL Server connection strings with automatic fallback
CONNECTION_STRINGS = [
    (
        "Driver={ODBC Driver 17 for SQL Server};"
        "Server=.;"
        "Database=weddingorg;"
        "Trusted_Connection=yes;"
        "TrustServerCertificate=yes;"
    ),
    (
        "Driver={ODBC Driver 18 for SQL Server};"
        "Server=.;"
        "Database=weddingorg;"
        "Trusted_Connection=yes;"
        "TrustServerCertificate=yes;"
    ),
    (
        "Driver={SQL Server};"
        "Server=.;"
        "Database=weddingorg;"
        "Trusted_Connection=yes;"
    ),
]

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://localhost:11434")
DEFAULT_MODELS = ["llama3.2", "qwen2.5:0.5b", "llama3"]

SERVICE_TYPE_MAP = {
    1: "Venue (Full Day)",
    2: "Catering (Hourly)",
    3: "Photography (Hourly)",
    4: "Decoration (Hourly)",
    5: "Music & Entertainment (Hourly)",
    6: "Transportation (Hourly)",
    7: "Other (Hourly)",
}


class MessageInput(BaseModel):
    messagetxt: str
    user_id: Optional[str] = None
    user_name: Optional[str] = None
    user_email: Optional[str] = None


class BookingRequest(BaseModel):
    user_id: str
    service_id: str
    booking_date: str  # YYYY-MM-DD
    start_hour: Optional[int] = None
    end_hour: Optional[int] = None
    notes: Optional[str] = None


def get_db_connection():
    """Attempts to connect to SQL Server using available drivers."""
    last_error = None
    for conn_str in CONNECTION_STRINGS:
        try:
            conn = pyodbc.connect(conn_str, timeout=5)
            return conn
        except Exception as e:
            last_error = e
    raise HTTPException(status_code=500, detail=f"Database connection failed: {last_error}")


def get_all_services() -> List[Dict[str, Any]]:
    """Fetches all active services from the database."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute("""
            SELECT Id, ServiceType, Service_Name, Price, Capacity, City, Address, Description, MainImage
            FROM dbo.Services;
        """)
        rows = cursor.fetchall()
        services = []
        for r in rows:
            services.append({
                "id": str(r[0]),
                "service_type": r[1],
                "service_type_name": SERVICE_TYPE_MAP.get(r[1], "Service"),
                "service_name": r[2] or "",
                "price": float(r[3]) if r[3] is not None else 0.0,
                "capacity": r[4],
                "city": r[5] or "",
                "address": r[6] or "",
                "description": r[7] or "",
                "main_image": r[8] or "",
                "is_venue": (r[1] == 1)
            })
        return services
    finally:
        cursor.close()
        conn.close()


def get_service_by_name_or_id(identifier: str) -> Optional[Dict[str, Any]]:
    """Finds a service by exact/fuzzy name or UUID."""
    services = get_all_services()
    clean_id = identifier.strip().lower()

    # Exact match on UUID
    for s in services:
        if s["id"].lower() == clean_id:
            return s

    # Exact match on Name
    for s in services:
        if s["service_name"].lower() == clean_id:
            return s

    # Fuzzy substring match
    for s in services:
        if clean_id in s["service_name"].lower() or s["service_name"].lower() in clean_id:
            return s

    return None


def get_booked_slots_for_service(service_id: str, booking_date: datetime.date) -> List[Dict[str, Any]]:
    """Returns all booked time slots for a service on a given date."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute("""
            SELECT BookingId, UserId, BookingDate, StartHour, EndHour, Status, PaymentStatus, TotalPrice
            FROM dbo.Bookings
            WHERE ServiceId = ?
              AND CAST(BookingDate AS DATE) = ?
              AND Status IN ('Confirmed', 'PendingPayment')
              AND PaymentStatus = 'Paid';
        """, (service_id, booking_date))
        rows = cursor.fetchall()
        booked = []
        for r in rows:
            booked.append({
                "booking_id": str(r[0]),
                "user_id": r[1],
                "booking_date": str(r[2]),
                "start_hour": r[3],
                "end_hour": r[4],
                "status": r[5],
                "payment_status": r[6],
                "total_price": float(r[7]) if r[7] is not None else 0.0
            })
        return booked
    finally:
        cursor.close()
        conn.close()


def calculate_available_slots(service: Dict[str, Any], date_obj: datetime.date) -> Dict[str, Any]:
    """Calculates available slots/hours for a service on a specific date."""
    is_venue = service.get("is_venue", False)
    booked = get_booked_slots_for_service(service["id"], date_obj)

    if is_venue:
        is_available = len(booked) == 0
        return {
            "service_id": service["id"],
            "service_name": service["service_name"],
            "service_type": service["service_type_name"],
            "date": str(date_obj),
            "is_venue": True,
            "is_available": is_available,
            "available_slots": ["Full Day (All Day Available)"] if is_available else [],
            "booked_slots": ["Fully Booked"] if not is_available else []
        }
    else:
        # Standard hours 09:00 to 23:00
        working_hours = list(range(9, 23))  # 9, 10, ... 22
        booked_hours = set()
        for b in booked:
            sh = b.get("start_hour")
            eh = b.get("end_hour")
            if sh is not None and eh is not None:
                for h in range(sh, eh):
                    booked_hours.add(h)

        free_hours = [h for h in working_hours if h not in booked_hours]

        # Group consecutive free hours into slot ranges
        available_slots = []
        if free_hours:
            slot_start = free_hours[0]
            slot_prev = free_hours[0]
            for h in free_hours[1:]:
                if h == slot_prev + 1:
                    slot_prev = h
                else:
                    available_slots.append(f"{slot_start:02d}:00 – {(slot_prev + 1):02d}:00")
                    slot_start = h
                    slot_prev = h
            available_slots.append(f"{slot_start:02d}:00 – {(slot_prev + 1):02d}:00")

        booked_slot_texts = [
            f"{b['start_hour']:02d}:00 – {b['end_hour']:02d}:00"
            for b in booked
            if b.get("start_hour") is not None and b.get("end_hour") is not None
        ]

        return {
            "service_id": service["id"],
            "service_name": service["service_name"],
            "service_type": service["service_type_name"],
            "date": str(date_obj),
            "is_venue": False,
            "is_available": len(free_hours) > 0,
            "available_slots": available_slots if available_slots else ["No slots available"],
            "booked_slots": booked_slot_texts
        }


def check_booking_conflict(service_id: str, is_venue: bool, date_obj: datetime.date, start_hour: Optional[int], end_hour: Optional[int]) -> bool:
    """Returns True if there is a scheduling conflict in the database."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        if is_venue:
            cursor.execute("""
                SELECT COUNT(*) FROM dbo.Bookings
                WHERE ServiceId = ?
                  AND CAST(BookingDate AS DATE) = ?
                  AND Status IN ('Confirmed', 'PendingPayment')
                  AND PaymentStatus = 'Paid';
            """, (service_id, date_obj))
            count = cursor.fetchone()[0]
            return count > 0
        else:
            if start_hour is None or end_hour is None or end_hour <= start_hour:
                return True
            cursor.execute("""
                SELECT COUNT(*) FROM dbo.Bookings
                WHERE ServiceId = ?
                  AND CAST(BookingDate AS DATE) = ?
                  AND Status IN ('Confirmed', 'PendingPayment')
                  AND PaymentStatus = 'Paid'
                  AND StartHour < ?
                  AND EndHour > ?;
            """, (service_id, date_obj, end_hour, start_hour))
            count = cursor.fetchone()[0]
            return count > 0
    finally:
        cursor.close()
        conn.close()


def insert_booking_to_db(user_id: str, service: Dict[str, Any], date_obj: datetime.date, start_hour: Optional[int] = None, end_hour: Optional[int] = None, notes: Optional[str] = None) -> Dict[str, Any]:
    """Inserts a new confirmed booking into dbo.Bookings."""
    is_venue = service["is_venue"]
    unit_price = service["price"]

    if is_venue:
        total_price = unit_price
        start_hour_val = None
        end_hour_val = None
    else:
        if start_hour is None:
            start_hour = 9
        if end_hour is None or end_hour <= start_hour:
            end_hour = start_hour + 2
        total_price = unit_price * (end_hour - start_hour)
        start_hour_val = start_hour
        end_hour_val = end_hour

    # Verify conflict
    if check_booking_conflict(service["id"], is_venue, date_obj, start_hour_val, end_hour_val):
        raise ValueError("Selected date or time slot is already booked. Please choose another time.")

    booking_id = str(uuid.uuid4())
    created_at = datetime.datetime.utcnow()
    status = "Confirmed"
    payment_status = "Paid"
    notes_val = notes or "Booked via WeddingAI Chatbot"

    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        query = """
            INSERT INTO dbo.Bookings (
                BookingId, UserId, ServiceId, BookingDate, StartHour, EndHour,
                TotalPrice, Status, PaymentStatus, StripeSessionId, Notes, CreatedAt
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        """
        cursor.execute(query, (
            booking_id,
            user_id,
            service["id"],
            date_obj,
            start_hour_val,
            end_hour_val,
            total_price,
            status,
            payment_status,
            "ai_chat_confirmed",
            notes_val,
            created_at
        ))
        conn.commit()

        return {
            "booking_id": booking_id,
            "user_id": user_id,
            "service_name": service["service_name"],
            "service_type": service["service_type_name"],
            "date": str(date_obj),
            "start_hour": start_hour_val,
            "end_hour": end_hour_val,
            "total_price": total_price,
            "status": status,
            "payment_status": payment_status
        }
    finally:
        cursor.close()
        conn.close()


def query_ollama(prompt: str) -> str:
    """Queries the local Ollama instance with model fallback and fast liveness detection."""
    # Fast liveness check
    try:
        ping = requests.get(f"{OLLAMA_URL}/api/tags", timeout=1.5)
        if ping.status_code != 200:
            return ""
        available_models = [m.get("name", "") for m in ping.json().get("models", [])]
    except Exception:
        return ""

    # Choose best available model or try defaults
    models_to_try = [m for m in DEFAULT_MODELS if any(m in am for am in available_models)] or DEFAULT_MODELS

    for model_name in models_to_try:
        try:
            res = requests.post(
                f"{OLLAMA_URL}/api/generate",
                json={"model": model_name, "prompt": prompt, "stream": False},
                timeout=15
            )
            if res.status_code == 200:
                data = res.json()
                return data.get("response", "").strip()
        except Exception:
            continue
    return ""


@app.get("/test-db")
def test_db():
    try:
        services = get_all_services()
        return {
            "status": "Success",
            "message": "Connected Successfully to SQL Server!",
            "services_count": len(services),
            "sample_services": services[:3]
        }
    except Exception as e:
        return {"status": "Error", "error_details": str(e)}


@app.get("/services")
def list_services():
    try:
        services = get_all_services()
        return {"status": "Success", "services": services}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/availability")
def get_availability(service_name: Optional[str] = None, service_id: Optional[str] = None, date: Optional[str] = None):
    try:
        service = None
        if service_id:
            service = get_service_by_name_or_id(service_id)
        elif service_name:
            service = get_service_by_name_or_id(service_name)

        if not service:
            raise HTTPException(status_code=404, detail="Service not found")

        target_date = datetime.date.today() + datetime.timedelta(days=1)
        if date:
            try:
                target_date = datetime.datetime.strptime(date, "%Y-%m-%d").date()
            except ValueError:
                raise HTTPException(status_code=400, detail="Invalid date format, use YYYY-MM-DD")

        avail = calculate_available_slots(service, target_date)
        return {"status": "Success", "availability": avail}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/book")
def direct_book(req: BookingRequest):
    try:
        service = get_service_by_name_or_id(req.service_id)
        if not service:
            raise HTTPException(status_code=404, detail="Service not found")

        try:
            target_date = datetime.datetime.strptime(req.booking_date, "%Y-%m-%d").date()
        except ValueError:
            raise HTTPException(status_code=400, detail="Invalid date format, use YYYY-MM-DD")

        result = insert_booking_to_db(
            user_id=req.user_id,
            service=service,
            date_obj=target_date,
            start_hour=req.start_hour,
            end_hour=req.end_hour,
            notes=req.notes
        )
        return {"status": "Success", "booking": result}
    except ValueError as ve:
        raise HTTPException(status_code=400, detail=str(ve))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/ask-ai")
def ask_ai(data: MessageInput):
    try:
        now_dt = datetime.datetime.now()
        today_str = now_dt.strftime("%Y-%m-%d")
        tomorrow_str = (now_dt + datetime.timedelta(days=1)).strftime("%Y-%m-%d")

        user_text = data.messagetxt.strip()
        user_id = data.user_id.strip() if data.user_id else None
        user_name = data.user_name.strip() if data.user_name else "Valued Guest"
        is_logged_in = bool(user_id)

        # 1. Fetch live services
        services = get_all_services()

        # 2. Build context for services and availability
        services_text = ""
        for s in services:
            services_text += f"- ID: {s['id']} | Name: {s['service_name']} | Type: {s['service_type_name']} | Price: ${s['price']:,.2f} | City: {s['city']} | Capacity: {s.get('capacity') or 'N/A'}\n"

        # 3. Detect intent (Rule-based pre-processing & AI assistance)
        user_lower = user_text.lower()

        # Date extraction heuristic (YYYY-MM-DD or 'tomorrow' or 'today' or '2026-XX-XX')
        date_match = re.search(r"\b(202\d[-/]\d{1,2}[-/]\d{1,2})\b", user_text)
        extracted_date = None
        if date_match:
            try:
                extracted_date = datetime.datetime.strptime(date_match.group(1).replace("/", "-"), "%Y-%m-%d").date()
            except Exception:
                pass
        elif "tomorrow" in user_lower or "غدا" in user_lower or "بكره" in user_lower or "بكرة" in user_lower:
            extracted_date = (now_dt + datetime.timedelta(days=1)).date()
        elif "today" in user_lower or "اليوم" in user_lower:
            extracted_date = now_dt.date()

        # Identify mentioned service if any
        matched_service = None
        for s in services:
            if s["service_name"].lower() in user_lower or s["id"].lower() in user_lower:
                matched_service = s
                break
        if not matched_service:
            for s in services:
                words = [w for w in s["service_name"].lower().split() if len(w) > 3]
                if any(w in user_lower for w in words):
                    matched_service = s
                    break

        # Check if user is asking for available slots
        is_availability_query = any(w in user_lower for w in [
            "available", "slot", "slots", "availability", "free", "time", "date",
            "متاح", "المتاحة", "أوقات", "اوقات", "مواعيد", "حجز متاح", "الفاضي", "شاغر"
        ])

        # Check if user wants to BOOK
        is_booking_query = any(w in user_lower for w in [
            "book", "reserve", "احجز", "حجز", "اريد حجز", "أريد حجز", "بدي احجز", "booking"
        ]) and not ("هل" in user_lower or "متاح" in user_lower or "available" in user_lower and not any(k in user_lower for k in ["confirm", "please book", "احجزلي", "احجز لي"]))

        # Time range extraction (e.g. from 10 to 14, 14:00 - 18:00, 2 to 5)
        # Strip extracted date text first so YYYY-MM-DD digits don't get matched as time
        text_without_date = re.sub(r"\b202\d[-/]\d{1,2}[-/]\d{1,2}\b", "", user_text)
        start_hour_ex = None
        end_hour_ex = None

        time_match = re.search(
            r"(?:from|between|at|من|الساعة)?\s*(\d{1,2})(?::00)?\s*(?:to|-|and|till|until|إلى|الي|حتى|و)\s*(\d{1,2})(?::00)?",
            text_without_date,
            re.IGNORECASE
        )
        if time_match:
            try:
                sh = int(time_match.group(1))
                eh = int(time_match.group(2))
                if 0 <= sh <= 23 and 1 <= eh <= 24 and eh > sh:
                    start_hour_ex = sh
                    end_hour_ex = eh
            except Exception:
                pass

        # If user directly asks for booking action:
        if is_booking_query and matched_service:
            if not is_logged_in:
                return {
                    "status": "Success",
                    "ai_response": (
                        f"🔒 **Authentication Required for Booking**\n\n"
                        f"To complete your reservation for **{matched_service['service_name']}**, please sign in to your account first.\n\n"
                        f"👉 [Click here to Login](/Identity/Account/Login) or [Register a new account](/Identity/Account/Register)."
                    )
                }

            target_date = extracted_date or (now_dt + datetime.timedelta(days=1)).date()
            try:
                res = insert_booking_to_db(
                    user_id=user_id,
                    service=matched_service,
                    date_obj=target_date,
                    start_hour=start_hour_ex,
                    end_hour=end_hour_ex,
                    notes=f"Booked via AI Chatbot for {user_name}"
                )

                time_str = "Full Day (All Day)" if matched_service["is_venue"] else f"{res['start_hour']:02d}:00 – {res['end_hour']:02d}:00"
                confirmation_msg = (
                    f"🎉 **Congratulations {user_name}! Your booking is confirmed!**\n\n"
                    f"✨ **Service:** {res['service_name']} ({res['service_type']})\n"
                    f"📅 **Date:** {res['date']}\n"
                    f"⏰ **Time Slot:** {time_str}\n"
                    f"💵 **Total Price:** ${res['total_price']:,.2f}\n"
                    f"🔖 **Booking Reference:** `{res['booking_id']}`\n\n"
                    f"You can view and manage this reservation anytime in [My Bookings](/MyBookings)."
                )
                return {"status": "Success", "ai_response": confirmation_msg}

            except ValueError as ve:
                # Slot conflict or validation issue
                avail_info = calculate_available_slots(matched_service, target_date)
                avail_str = ", ".join(avail_info["available_slots"]) if avail_info["available_slots"] else "None (Fully Booked for this date)"
                return {
                    "status": "Success",
                    "ai_response": (
                        f"⚠️ **Slot Unavailable:** {str(ve)}\n\n"
                        f"Here are the available slots for **{matched_service['service_name']}** on **{target_date}**:\n"
                        f"🕒 {avail_str}\n\n"
                        f"Please specify a different time or date to proceed!"
                    )
                }
            except Exception as e:
                return {"status": "Error", "ai_response": f"Error completing booking: {str(e)}"}

        # If user asks about available slots/services
        availability_context = ""
        if matched_service:
            check_date = extracted_date or (now_dt + datetime.timedelta(days=1)).date()
            avail = calculate_available_slots(matched_service, check_date)
            availability_context = (
                f"\n--- Real-Time Slot Availability for '{matched_service['service_name']}' on {check_date} ---\n"
                f"Service Type: {avail['service_type']}\n"
                f"Available Slots: {', '.join(avail['available_slots']) if avail['available_slots'] else 'None'}\n"
                f"Booked Slots: {', '.join(avail['booked_slots']) if avail['booked_slots'] else 'None'}\n"
            )

        # 4. Prompt Ollama LLM
        prompt = f"""
[System Instructions]
You are WeddingAI, a luxury wedding concierge and booking assistant.
Today's date is: {today_str}. Tomorrow is: {tomorrow_str}.
User Status: {"Logged in as " + user_name + " (User ID: " + user_id + ")" if is_logged_in else "Guest (Not logged in)"}

Guidelines:
1. Respond in the same language as the user (e.g. Arabic, English).
2. Help users explore wedding venues, photography, catering, entertainment, and decor services.
3. When the user asks about available slots or dates, provide clear details from the database context.
4. If a guest wants to book without being logged in, politely inform them to log in at /Identity/Account/Login.
5. If a logged-in user explicitly requests to book a specific service, output JSON in this exact structure:
{{"action": "BOOK", "service_name": "Exact Service Name", "date": "YYYY-MM-DD", "start_hour": 10, "end_hour": 14, "notes": "optional notes"}}

[Available Services Catalog]
{services_text}
{availability_context}

[User Message]
{user_text}
"""
        reply = query_ollama(prompt)

        # 5. Check if LLM outputted a structured booking action
        if '{"action": "BOOK"' in reply or '"action": "BOOK"' in reply:
            try:
                json_start = reply.find('{')
                json_end = reply.rfind('}') + 1
                json_str = reply[json_start:json_end]
                data_json = json.loads(json_str)

                svc_name = data_json.get("service_name") or data_json.get("service")
                svc = get_service_by_name_or_id(svc_name) if svc_name else matched_service

                if not svc:
                    return {"status": "Success", "ai_response": f"I couldn't identify the service '{svc_name}'. Please choose from our catalog!"}

                if not is_logged_in:
                    return {
                        "status": "Success",
                        "ai_response": (
                            f"🔒 **Authentication Required**\n\n"
                            f"To book **{svc['service_name']}**, please sign in first: [Login Here](/Identity/Account/Login)."
                        )
                    }

                b_date_str = data_json.get("date") or (str(extracted_date) if extracted_date else tomorrow_str)
                b_date = datetime.datetime.strptime(b_date_str, "%Y-%m-%d").date()
                sh = data_json.get("start_hour") or start_hour_ex
                eh = data_json.get("end_hour") or end_hour_ex

                res = insert_booking_to_db(
                    user_id=user_id,
                    service=svc,
                    date_obj=b_date,
                    start_hour=sh,
                    end_hour=eh,
                    notes=data_json.get("notes")
                )

                time_str = "Full Day (All Day)" if svc["is_venue"] else f"{res['start_hour']:02d}:00 – {res['end_hour']:02d}:00"
                return {
                    "status": "Success",
                    "ai_response": (
                        f"🎉 **Congratulations {user_name}! Your booking is confirmed!**\n\n"
                        f"✨ **Service:** {res['service_name']} ({res['service_type']})\n"
                        f"📅 **Date:** {res['date']}\n"
                        f"⏰ **Time Slot:** {time_str}\n"
                        f"💵 **Total Price:** ${res['total_price']:,.2f}\n"
                        f"🔖 **Booking Reference:** `{res['booking_id']}`\n\n"
                        f"You can view and manage this reservation anytime in [My Bookings](/MyBookings)."
                    )
                }

            except ValueError as ve:
                return {"status": "Success", "ai_response": f"⚠️ Slot Conflict: {str(ve)}"}
            except Exception as e:
                pass  # Fall back to standard AI text response

        # Fallback if Ollama wasn't running or returned empty reply:
        if not reply:
            if matched_service and is_availability_query:
                check_date = extracted_date or (now_dt + datetime.timedelta(days=1)).date()
                avail = calculate_available_slots(matched_service, check_date)
                slots_str = "\n".join([f"- {s}" for s in avail["available_slots"]])
                reply = (
                    f"✨ **Availability for {matched_service['service_name']}** on **{check_date}**:\n\n"
                    f"**Service Type:** {avail['service_type']}\n"
                    f"**Available Slots:**\n{slots_str}\n\n"
                    f"To book this service, reply with: *Book {matched_service['service_name']} on {check_date}*"
                )
            else:
                reply = (
                    f"مرحباً بك {user_name}! أنا مساعدك الذكي WeddingAI. 🌸\n\n"
                    f"لدينا {len(services)} من الخدمات والأماكن الفاخرة المتاحة للحجز.\n"
                    f"يمكنك الاستفسار عن المواعيد المتاحة لأي قاعة أو خدمة، أو طلب الحجز مباشرة!"
                )

        return {"status": "Success", "ai_response": reply}

    except Exception as e:
        return {"status": "Error", "ai_response": f"Error: {str(e)}"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
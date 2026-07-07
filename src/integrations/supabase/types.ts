export type Json =
  | string
  | number
  | boolean
  | null
  | { [key: string]: Json | undefined }
  | Json[]

export type Database = {
  // Allows to automatically instantiate createClient with right options
  // instead of createClient<Database, { PostgrestVersion: 'XX' }>(URL, KEY)
  __InternalSupabase: {
    PostgrestVersion: "14.5"
  }
  public: {
    Tables: {
      account_settings: {
        Row: {
          acw_in: boolean
          acw_out: boolean
          allow_reject: boolean
          allow_transfer_away: boolean
          auto_answer: boolean
          auto_answer_secs: number
          auto_assign: boolean
          auto_tagging: boolean
          away_status: string
          billing_address: string
          billing_country: string
          billing_emails: string
          call_tags: string
          created_at: string
          default_country: string
          dialer_method: string
          dialer_url: string
          domains: string
          id: string
          internal_timeout: boolean
          invoice_name: string
          ivr_timeout: number
          limit_ivr: boolean
          notify_on_agent_changes: boolean
          number_format: string
          org_name: string
          outbound_ring_limit: boolean
          param_name: string
          payment_methods: Json
          phone_numbers: Json
          registration_number: string
          send_invoices_to_admins: boolean
          show_inbound: boolean
          updated_at: string
          user_id: string
          vat_number: string
          wait_time: number
        }
        Insert: {
          acw_in?: boolean
          acw_out?: boolean
          allow_reject?: boolean
          allow_transfer_away?: boolean
          auto_answer?: boolean
          auto_answer_secs?: number
          auto_assign?: boolean
          auto_tagging?: boolean
          away_status?: string
          billing_address?: string
          billing_country?: string
          billing_emails?: string
          call_tags?: string
          created_at?: string
          default_country?: string
          dialer_method?: string
          dialer_url?: string
          domains?: string
          id?: string
          internal_timeout?: boolean
          invoice_name?: string
          ivr_timeout?: number
          limit_ivr?: boolean
          notify_on_agent_changes?: boolean
          number_format?: string
          org_name?: string
          outbound_ring_limit?: boolean
          param_name?: string
          payment_methods?: Json
          phone_numbers?: Json
          registration_number?: string
          send_invoices_to_admins?: boolean
          show_inbound?: boolean
          updated_at?: string
          user_id: string
          vat_number?: string
          wait_time?: number
        }
        Update: {
          acw_in?: boolean
          acw_out?: boolean
          allow_reject?: boolean
          allow_transfer_away?: boolean
          auto_answer?: boolean
          auto_answer_secs?: number
          auto_assign?: boolean
          auto_tagging?: boolean
          away_status?: string
          billing_address?: string
          billing_country?: string
          billing_emails?: string
          call_tags?: string
          created_at?: string
          default_country?: string
          dialer_method?: string
          dialer_url?: string
          domains?: string
          id?: string
          internal_timeout?: boolean
          invoice_name?: string
          ivr_timeout?: number
          limit_ivr?: boolean
          notify_on_agent_changes?: boolean
          number_format?: string
          org_name?: string
          outbound_ring_limit?: boolean
          param_name?: string
          payment_methods?: Json
          phone_numbers?: Json
          registration_number?: string
          send_invoices_to_admins?: boolean
          show_inbound?: boolean
          updated_at?: string
          user_id?: string
          vat_number?: string
          wait_time?: number
        }
        Relationships: []
      }
      calls: {
        Row: {
          active_seconds: number
          agent_id: string | null
          answered_at: string | null
          auto_tag_ids: string[]
          call_id: string | null
          called: string
          caller: string
          created_at: string
          direction: string
          ended_at: string | null
          from_display: string | null
          from_uri: string | null
          full_transcript: string | null
          group_id: string | null
          hangup_cause: string | null
          has_recording: boolean
          hold_seconds: number
          id: string
          inputs: string
          notes: string
          recording_url: string | null
          ring_seconds: number
          sentiment: string | null
          started_at: string
          status: string
          summary: string | null
          summary_accuracy_feedback: string | null
          summary_language: string | null
          tag_ids: string[]
          tenant_id: string
          to_display: string | null
          to_uri: string | null
          total_hold_seconds: number
          total_seconds: number
          updated_at: string
          user_id: string
        }
        Insert: {
          active_seconds?: number
          agent_id?: string | null
          answered_at?: string | null
          auto_tag_ids?: string[]
          call_id?: string | null
          called?: string
          caller?: string
          created_at?: string
          direction: string
          ended_at?: string | null
          from_display?: string | null
          from_uri?: string | null
          full_transcript?: string | null
          group_id?: string | null
          hangup_cause?: string | null
          has_recording?: boolean
          hold_seconds?: number
          id?: string
          inputs?: string
          notes?: string
          recording_url?: string | null
          ring_seconds?: number
          sentiment?: string | null
          started_at: string
          status: string
          summary?: string | null
          summary_accuracy_feedback?: string | null
          summary_language?: string | null
          tag_ids?: string[]
          tenant_id: string
          to_display?: string | null
          to_uri?: string | null
          total_hold_seconds?: number
          total_seconds?: number
          updated_at?: string
          user_id: string
        }
        Update: {
          active_seconds?: number
          agent_id?: string | null
          answered_at?: string | null
          auto_tag_ids?: string[]
          call_id?: string | null
          called?: string
          caller?: string
          created_at?: string
          direction?: string
          ended_at?: string | null
          from_display?: string | null
          from_uri?: string | null
          full_transcript?: string | null
          group_id?: string | null
          hangup_cause?: string | null
          has_recording?: boolean
          hold_seconds?: number
          id?: string
          inputs?: string
          notes?: string
          recording_url?: string | null
          ring_seconds?: number
          sentiment?: string | null
          started_at?: string
          status?: string
          summary?: string | null
          summary_accuracy_feedback?: string | null
          summary_language?: string | null
          tag_ids?: string[]
          tenant_id?: string
          to_display?: string | null
          to_uri?: string | null
          total_hold_seconds?: number
          total_seconds?: number
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      contacts: {
        Row: {
          company: string
          created_at: string
          email: string
          id: string
          last_call_at: string | null
          name: string
          notes: string
          phone: string
          tag_ids: string[]
          tenant_id: string
          total_calls: number
          updated_at: string
          user_id: string
        }
        Insert: {
          company?: string
          created_at?: string
          email?: string
          id?: string
          last_call_at?: string | null
          name: string
          notes?: string
          phone?: string
          tag_ids?: string[]
          tenant_id: string
          total_calls?: number
          updated_at?: string
          user_id: string
        }
        Update: {
          company?: string
          created_at?: string
          email?: string
          id?: string
          last_call_at?: string | null
          name?: string
          notes?: string
          phone?: string
          tag_ids?: string[]
          tenant_id?: string
          total_calls?: number
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      edit_logs: {
        Row: {
          action: string
          created_at: string
          entity_id: string
          entity_type: string
          field: string | null
          id: string
          metadata: Json
          new_value: Json | null
          old_value: Json | null
          summary: string | null
          tenant_id: string
          user_id: string
        }
        Insert: {
          action: string
          created_at?: string
          entity_id: string
          entity_type: string
          field?: string | null
          id?: string
          metadata?: Json
          new_value?: Json | null
          old_value?: Json | null
          summary?: string | null
          tenant_id: string
          user_id: string
        }
        Update: {
          action?: string
          created_at?: string
          entity_id?: string
          entity_type?: string
          field?: string | null
          id?: string
          metadata?: Json
          new_value?: Json | null
          old_value?: Json | null
          summary?: string | null
          tenant_id?: string
          user_id?: string
        }
        Relationships: []
      }
      flows: {
        Row: {
          assigned_extension: string | null
          created_at: string
          description: string
          edges: Json
          id: string
          name: string
          nodes: Json
          status: Database["public"]["Enums"]["flow_status"]
          tenant_id: string
          updated_at: string
          user_id: string
        }
        Insert: {
          assigned_extension?: string | null
          created_at?: string
          description?: string
          edges?: Json
          id?: string
          name: string
          nodes?: Json
          status?: Database["public"]["Enums"]["flow_status"]
          tenant_id: string
          updated_at?: string
          user_id: string
        }
        Update: {
          assigned_extension?: string | null
          created_at?: string
          description?: string
          edges?: Json
          id?: string
          name?: string
          nodes?: Json
          status?: Database["public"]["Enums"]["flow_status"]
          tenant_id?: string
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      invoices: {
        Row: {
          amount: number
          created_at: string
          currency: string
          due_date: string | null
          id: string
          invoice_number: string
          issue_date: string
          paid_at: string | null
          pdf_url: string | null
          status: Database["public"]["Enums"]["invoice_status"]
          tenant_id: string
          updated_at: string
          user_id: string
        }
        Insert: {
          amount?: number
          created_at?: string
          currency?: string
          due_date?: string | null
          id?: string
          invoice_number: string
          issue_date?: string
          paid_at?: string | null
          pdf_url?: string | null
          status?: Database["public"]["Enums"]["invoice_status"]
          tenant_id: string
          updated_at?: string
          user_id: string
        }
        Update: {
          amount?: number
          created_at?: string
          currency?: string
          due_date?: string | null
          id?: string
          invoice_number?: string
          issue_date?: string
          paid_at?: string | null
          pdf_url?: string | null
          status?: Database["public"]["Enums"]["invoice_status"]
          tenant_id?: string
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      profiles: {
        Row: {
          browser_notifications: boolean
          created_at: string
          disabled: boolean
          display_name: string | null
          email: string | null
          extension_number: number | null
          first_name: string | null
          groups: string[]
          id: string
          language: string
          last_name: string | null
          outbound_cid: string | null
          record_inbound_external: boolean
          record_inbound_internal: boolean
          record_on_demand: boolean
          record_outbound_external: boolean
          record_outbound_internal: boolean
          role: string
          status: string
          tenant_id: string
          timezone: string
          updated_at: string
          user_id: string
        }
        Insert: {
          browser_notifications?: boolean
          created_at?: string
          disabled?: boolean
          display_name?: string | null
          email?: string | null
          extension_number?: number | null
          first_name?: string | null
          groups?: string[]
          id?: string
          language?: string
          last_name?: string | null
          outbound_cid?: string | null
          record_inbound_external?: boolean
          record_inbound_internal?: boolean
          record_on_demand?: boolean
          record_outbound_external?: boolean
          record_outbound_internal?: boolean
          role?: string
          status?: string
          tenant_id: string
          timezone?: string
          updated_at?: string
          user_id: string
        }
        Update: {
          browser_notifications?: boolean
          created_at?: string
          disabled?: boolean
          display_name?: string | null
          email?: string | null
          extension_number?: number | null
          first_name?: string | null
          groups?: string[]
          id?: string
          language?: string
          last_name?: string | null
          outbound_cid?: string | null
          record_inbound_external?: boolean
          record_inbound_internal?: boolean
          record_on_demand?: boolean
          record_outbound_external?: boolean
          record_outbound_internal?: boolean
          role?: string
          status?: string
          tenant_id?: string
          timezone?: string
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      queue_wrapup_codes: {
        Row: {
          created_at: string
          id: string
          queue_id: string
          tenant_id: string
          wrapup_code_id: string
        }
        Insert: {
          created_at?: string
          id?: string
          queue_id: string
          tenant_id: string
          wrapup_code_id: string
        }
        Update: {
          created_at?: string
          id?: string
          queue_id?: string
          tenant_id?: string
          wrapup_code_id?: string
        }
        Relationships: [
          {
            foreignKeyName: "queue_wrapup_codes_wrapup_code_id_fkey"
            columns: ["wrapup_code_id"]
            isOneToOne: false
            referencedRelation: "wrapup_codes"
            referencedColumns: ["id"]
          },
        ]
      }
      rbac_roles: {
        Row: {
          created_at: string
          description: string
          id: string
          is_system: boolean
          name: string
          permissions: Json
          status: string
          tenant_id: string | null
          updated_at: string
        }
        Insert: {
          created_at?: string
          description?: string
          id?: string
          is_system?: boolean
          name: string
          permissions?: Json
          status?: string
          tenant_id?: string | null
          updated_at?: string
        }
        Update: {
          created_at?: string
          description?: string
          id?: string
          is_system?: boolean
          name?: string
          permissions?: Json
          status?: string
          tenant_id?: string | null
          updated_at?: string
        }
        Relationships: []
      }
      rbac_user_roles: {
        Row: {
          created_at: string
          id: string
          role_id: string
          tenant_id: string
          user_id: string
        }
        Insert: {
          created_at?: string
          id?: string
          role_id: string
          tenant_id: string
          user_id: string
        }
        Update: {
          created_at?: string
          id?: string
          role_id?: string
          tenant_id?: string
          user_id?: string
        }
        Relationships: [
          {
            foreignKeyName: "rbac_user_roles_role_id_fkey"
            columns: ["role_id"]
            isOneToOne: false
            referencedRelation: "rbac_roles"
            referencedColumns: ["id"]
          },
        ]
      }
      sip_accounts: {
        Row: {
          auth_id: string
          created_at: string
          display_name: string
          id: string
          is_active: boolean
          sip_uri: string
          stun_urls: string[]
          tenant_id: string
          turn_url: string
          turn_username: string
          updated_at: string
          user_id: string
          ws_url: string
        }
        Insert: {
          auth_id?: string
          created_at?: string
          display_name?: string
          id?: string
          is_active?: boolean
          sip_uri?: string
          stun_urls?: string[]
          tenant_id: string
          turn_url?: string
          turn_username?: string
          updated_at?: string
          user_id: string
          ws_url?: string
        }
        Update: {
          auth_id?: string
          created_at?: string
          display_name?: string
          id?: string
          is_active?: boolean
          sip_uri?: string
          stun_urls?: string[]
          tenant_id?: string
          turn_url?: string
          turn_username?: string
          updated_at?: string
          user_id?: string
          ws_url?: string
        }
        Relationships: []
      }
      softphone_call_logs: {
        Row: {
          contact_id: string | null
          created_at: string
          direction: string
          display_name: string
          duration_sec: number
          failure_reason: string
          id: string
          number: string
          started_at: string
          status: string
          tenant_id: string
          user_id: string
        }
        Insert: {
          contact_id?: string | null
          created_at?: string
          direction: string
          display_name?: string
          duration_sec?: number
          failure_reason?: string
          id?: string
          number: string
          started_at?: string
          status: string
          tenant_id: string
          user_id: string
        }
        Update: {
          contact_id?: string | null
          created_at?: string
          direction?: string
          display_name?: string
          duration_sec?: number
          failure_reason?: string
          id?: string
          number?: string
          started_at?: string
          status?: string
          tenant_id?: string
          user_id?: string
        }
        Relationships: []
      }
      tags: {
        Row: {
          color: string
          created_at: string
          id: string
          label: string
          tenant_id: string
          updated_at: string
          user_id: string
        }
        Insert: {
          color?: string
          created_at?: string
          id?: string
          label: string
          tenant_id: string
          updated_at?: string
          user_id: string
        }
        Update: {
          color?: string
          created_at?: string
          id?: string
          label?: string
          tenant_id?: string
          updated_at?: string
          user_id?: string
        }
        Relationships: []
      }
      voice_library: {
        Row: {
          created_at: string
          file_path: string | null
          id: string
          interruptible: boolean
          language: string
          name: string
          source: string
          tenant_id: string
          text: string | null
          updated_at: string
          url: string | null
          user_id: string
          voice: string
        }
        Insert: {
          created_at?: string
          file_path?: string | null
          id?: string
          interruptible?: boolean
          language?: string
          name: string
          source: string
          tenant_id: string
          text?: string | null
          updated_at?: string
          url?: string | null
          user_id: string
          voice?: string
        }
        Update: {
          created_at?: string
          file_path?: string | null
          id?: string
          interruptible?: boolean
          language?: string
          name?: string
          source?: string
          tenant_id?: string
          text?: string | null
          updated_at?: string
          url?: string | null
          user_id?: string
          voice?: string
        }
        Relationships: []
      }
      wrapup_codes: {
        Row: {
          category: string
          code: string
          color: string
          created_at: string
          id: string
          is_active: boolean
          label: string
          label_ar: string | null
          sort_order: number
          tenant_id: string
          updated_at: string
        }
        Insert: {
          category?: string
          code: string
          color?: string
          created_at?: string
          id?: string
          is_active?: boolean
          label: string
          label_ar?: string | null
          sort_order?: number
          tenant_id: string
          updated_at?: string
        }
        Update: {
          category?: string
          code?: string
          color?: string
          created_at?: string
          id?: string
          is_active?: boolean
          label?: string
          label_ar?: string | null
          sort_order?: number
          tenant_id?: string
          updated_at?: string
        }
        Relationships: []
      }
    }
    Views: {
      [_ in never]: never
    }
    Functions: {
      current_tenant_id: { Args: never; Returns: string }
      has_role: {
        Args: {
          _role: Database["public"]["Enums"]["app_role"]
          _user_id: string
        }
        Returns: boolean
      }
      is_tenant_admin: { Args: never; Returns: boolean }
      is_tenant_owner: { Args: never; Returns: boolean }
    }
    Enums: {
      app_role: "admin" | "user"
      flow_status: "draft" | "published"
      invoice_status: "paid" | "unpaid" | "overdue" | "refunded"
    }
    CompositeTypes: {
      [_ in never]: never
    }
  }
}

type DatabaseWithoutInternals = Omit<Database, "__InternalSupabase">

type DefaultSchema = DatabaseWithoutInternals[Extract<keyof Database, "public">]

export type Tables<
  DefaultSchemaTableNameOrOptions extends
    | keyof (DefaultSchema["Tables"] & DefaultSchema["Views"])
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof (DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"] &
        DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Views"])
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? (DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"] &
      DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Views"])[TableName] extends {
      Row: infer R
    }
    ? R
    : never
  : DefaultSchemaTableNameOrOptions extends keyof (DefaultSchema["Tables"] &
        DefaultSchema["Views"])
    ? (DefaultSchema["Tables"] &
        DefaultSchema["Views"])[DefaultSchemaTableNameOrOptions] extends {
        Row: infer R
      }
      ? R
      : never
    : never

export type TablesInsert<
  DefaultSchemaTableNameOrOptions extends
    | keyof DefaultSchema["Tables"]
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"]
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"][TableName] extends {
      Insert: infer I
    }
    ? I
    : never
  : DefaultSchemaTableNameOrOptions extends keyof DefaultSchema["Tables"]
    ? DefaultSchema["Tables"][DefaultSchemaTableNameOrOptions] extends {
        Insert: infer I
      }
      ? I
      : never
    : never

export type TablesUpdate<
  DefaultSchemaTableNameOrOptions extends
    | keyof DefaultSchema["Tables"]
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"]
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"][TableName] extends {
      Update: infer U
    }
    ? U
    : never
  : DefaultSchemaTableNameOrOptions extends keyof DefaultSchema["Tables"]
    ? DefaultSchema["Tables"][DefaultSchemaTableNameOrOptions] extends {
        Update: infer U
      }
      ? U
      : never
    : never

export type Enums<
  DefaultSchemaEnumNameOrOptions extends
    | keyof DefaultSchema["Enums"]
    | { schema: keyof DatabaseWithoutInternals },
  EnumName extends DefaultSchemaEnumNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaEnumNameOrOptions["schema"]]["Enums"]
    : never = never,
> = DefaultSchemaEnumNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? DatabaseWithoutInternals[DefaultSchemaEnumNameOrOptions["schema"]]["Enums"][EnumName]
  : DefaultSchemaEnumNameOrOptions extends keyof DefaultSchema["Enums"]
    ? DefaultSchema["Enums"][DefaultSchemaEnumNameOrOptions]
    : never

export type CompositeTypes<
  PublicCompositeTypeNameOrOptions extends
    | keyof DefaultSchema["CompositeTypes"]
    | { schema: keyof DatabaseWithoutInternals },
  CompositeTypeName extends PublicCompositeTypeNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[PublicCompositeTypeNameOrOptions["schema"]]["CompositeTypes"]
    : never = never,
> = PublicCompositeTypeNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? DatabaseWithoutInternals[PublicCompositeTypeNameOrOptions["schema"]]["CompositeTypes"][CompositeTypeName]
  : PublicCompositeTypeNameOrOptions extends keyof DefaultSchema["CompositeTypes"]
    ? DefaultSchema["CompositeTypes"][PublicCompositeTypeNameOrOptions]
    : never

export const Constants = {
  public: {
    Enums: {
      app_role: ["admin", "user"],
      flow_status: ["draft", "published"],
      invoice_status: ["paid", "unpaid", "overdue", "refunded"],
    },
  },
} as const
